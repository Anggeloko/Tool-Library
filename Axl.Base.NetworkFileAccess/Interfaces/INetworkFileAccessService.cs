﻿using System.Collections.Generic;
using Axl.Base.Models;

namespace Axl.Base.NetworkFileAccess.Interfaces
{
    /// <summary>
    /// Define las operaciones para el acceso seguro a archivos y carpetas en red.
    /// </summary>
    public interface INetworkFileAccessService
    {
        /// <summary>
        /// Lee el contenido de un archivo en red.
        /// </summary>
        byte[] ReadFile(string path);

        /// <summary>
        /// Escribe datos en un archivo en red.
        /// </summary>
        void WriteFile(string path, byte[] data);

        /// <summary>
        /// Lista los archivos en un directorio de red.
        /// </summary>
        string[] ListFiles(string path);

        /// <summary>
        /// Copia un archivo entre rutas de red.
        /// </summary>
        void Copy(string sourcePath, string destPath, bool overwrite = true);

        /// <summary>
        /// Mueve un archivo entre rutas de red.
        /// </summary>
        void Move(string sourcePath, string destPath);

        /// <summary>
        /// Elimina un archivo en red.
        /// </summary>
        void Delete(string path);

        /// <summary>
        /// Prueba los permisos de acceso en una ruta de red.
        /// </summary>
        DirectoryPermissions TestAccess(string path);

        /// <summary>
        /// Ruta por defecto para realizar la verificación de salud del servicio (via ICheckable).
        /// </summary>
        string CheckPath { get; set; }
    }
}

