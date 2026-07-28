﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Security;
using Axl.Base.Statics;
using Axl.Base.NetworkFileAccess.Interfaces;

namespace Axl.Base.NetworkFileAccess.Services
{
    /// <summary>
    /// Implementación del servicio de acceso a archivos en red utilizando suplantación de identidad.
    /// </summary>
    public class NetworkFileAccessService : INetworkFileAccessService, ICheckable
    {
        private readonly UserSecurityContext _context;

        /// <summary>
        /// Inicializa una nueva instancia del servicio con un contexto de seguridad específico.
        /// </summary>
        /// <param name="context">Contexto de seguridad (usuario suplantado) para las operaciones.</param>
        public NetworkFileAccessService(UserSecurityContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public byte[] ReadFile(string path)
        {
            var isNet = path.StartsWith(@"\\");
            var validatedPath = isNet ? WindowsHelper.ValidateNetworkPath(path) : Path.GetFullPath(path);
            
            if (isNet)
                return _context.Run(() => File.ReadAllBytes(validatedPath));
            else
                return File.ReadAllBytes(validatedPath);
        }

        public void WriteFile(string path, byte[] data)
        {
            var isNet = path.StartsWith(@"\\");
            var validatedPath = isNet ? WindowsHelper.ValidateNetworkPath(path) : Path.GetFullPath(path);
            
            var action = (Action)(() => 
            {
                var dir = Path.GetDirectoryName(validatedPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                File.WriteAllBytes(validatedPath, data);
            });

            if (isNet)
                _context.Run(action);
            else
                action();
        }

        public string[] ListFiles(string path)
        {
            var isNet = path.StartsWith(@"\\");
            var validatedPath = isNet ? WindowsHelper.ValidateNetworkPath(path) : Path.GetFullPath(path);
            
            if (isNet)
                return _context.Run(() => Directory.GetFiles(validatedPath));
            else
                return Directory.GetFiles(validatedPath);
        }

        public void Copy(string sourcePath, string destPath, bool overwrite = true)
        {
            var isSourceNet = sourcePath.StartsWith(@"\\");
            var isDestNet = destPath.StartsWith(@"\\");

            string vSource = isSourceNet ? WindowsHelper.ValidateNetworkPath(sourcePath) : Path.GetFullPath(sourcePath);
            string vDest = isDestNet ? WindowsHelper.ValidateNetworkPath(destPath) : Path.GetFullPath(destPath);

            if (isSourceNet && isDestNet)
            {
                // Red -> Red: Ambos bajo el mismo contexto suplantado
                _context.Run(() => File.Copy(vSource, vDest, overwrite));
            }
            else if (isSourceNet && !isDestNet)
            {
                // Red -> Local: Puente de bytes (Leemos suplantados, escribimos local)
                var data = _context.Run(() => File.ReadAllBytes(vSource));
                File.WriteAllBytes(vDest, data);
            }
            else if (!isSourceNet && isDestNet)
            {
                // Local -> Red: Puente de bytes (Leemos local, escribimos suplantados)
                var data = File.ReadAllBytes(vSource);
                _context.Run(() => 
                {
                    string dir = Path.GetDirectoryName(vDest);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllBytes(vDest, data);
                });
            }
            else
            {
                // Local -> Local: Operación estándar
                File.Copy(vSource, vDest, overwrite);
            }
        }

        public void Move(string sourcePath, string destPath)
        {
            // Usamos la misma lógica de Copy pero con Delete posterior si es híbrido
            var isSourceNet = sourcePath.StartsWith(@"\\");
            var isDestNet = destPath.StartsWith(@"\\");

            if (isSourceNet == isDestNet)
            {
                // Ambas en el mismo "mundo" (Red o Local)
                string vSource = isSourceNet ? WindowsHelper.ValidateNetworkPath(sourcePath) : Path.GetFullPath(sourcePath);
                string vDest = isDestNet ? WindowsHelper.ValidateNetworkPath(destPath) : Path.GetFullPath(destPath);

                if (isSourceNet)
                    _context.Run(() => File.Move(vSource, vDest));
                else
                    File.Move(vSource, vDest);
            }
            else
            {
                // Híbrido: Copiar y luego borrar
                Copy(sourcePath, destPath, true);
                Delete(sourcePath);
            }
        }

        public void Delete(string path)
        {
            var isNet = path.StartsWith(@"\\");
            var validatedPath = isNet ? WindowsHelper.ValidateNetworkPath(path) : Path.GetFullPath(path);
            
            if (isNet)
                _context.Run(() => File.Delete(validatedPath));
            else
                File.Delete(validatedPath);
        }

        public DirectoryPermissions TestAccess(string path)
        {
            var isNet = path.StartsWith(@"\\");
            var validatedPath = isNet ? WindowsHelper.ValidateNetworkPath(path) : Path.GetFullPath(path);
            
            Func<DirectoryPermissions> action = () =>
            {
                DirectoryPermissions perms = DirectoryPermissions.None;
                
                // Prueba de Listado
                try
                {
                    Directory.EnumerateFiles(validatedPath).FirstOrDefault();
                    perms |= DirectoryPermissions.List;
                }
                catch { }

                // Prueba de Escritura y Borrado
                var tempFile = Path.Combine(validatedPath, $"_test_access_{Guid.NewGuid()}.tmp");
                try
                {
                    File.WriteAllText(tempFile, "test_access");
                    perms |= DirectoryPermissions.Write;

                    File.Delete(tempFile);
                    perms |= DirectoryPermissions.Delete;
                }
                catch { }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }

                return perms;
            };

            if (isNet)
                return _context.Run(action);
            else
                return action();
        }

        /// <summary>
        /// Ruta por defecto para realizar la verificación de salud del servicio.
        /// </summary>
        public string CheckPath { get; set; }

        public async Task<Result<bool>> CheckAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (string.IsNullOrEmpty(CheckPath))
                        return Result<bool>.Success(true);

                    var isNet = CheckPath.StartsWith(@"\\");
                    var validatedPath = isNet ? WindowsHelper.ValidateNetworkPath(CheckPath) : Path.GetFullPath(CheckPath);

                    if (isNet)
                    {
                        return _context.Run(() =>
                        {
                            var exists = Directory.Exists(validatedPath);
                            return exists ? Result<bool>.Success(true) : Result<bool>.Failure($"Network path '{CheckPath}' is not accessible.");
                        });
                    }
                    else
                    {
                        bool exists = Directory.Exists(validatedPath);
                        return exists ? Result<bool>.Success(true) : Result<bool>.Failure($"Local path '{CheckPath}' does not exist.");
                    }
                });
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ExceptionUtils.Format(ex, "NetworkFileAccessCheck"));
            }
        }
    }
}

