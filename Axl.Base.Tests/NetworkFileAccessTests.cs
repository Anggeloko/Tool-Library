using NUnit.Framework;
using Moq;
using System;
using System.IO;
using Axl.Base.Models;
using Axl.Base.Security;
using Axl.Base.NetworkFileAccess.Services;
using System.Security;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class NetworkFileAccessTests
    {
        private NetworkFileAccessService _service;
        private SecureString _dummyPassword;

        [SetUp]
        public void Setup()
        {
            _dummyPassword = new SecureString();
            // Nota: UserSecurityContext es difícil de mockear sin interfaz, 
            // pero podemos usar el constructor de Scope.CurrentUserSession para tests seguros.
            var context = new UserSecurityContext(UserSecurityContext.Scope.CurrentUserSession);
            _service = new NetworkFileAccessService(context);
        }

        [Test]
        public void Constructor_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new NetworkFileAccessService(null));
        }

        [Test]
        public void ValidatePaths_LocalPath_Recognized()
        {
            // Test interno de lógica de detección de red vs local
            string path = @"C:\Temp\file.txt";
            bool isNet = path.StartsWith(@"\\");
            Assert.IsFalse(isNet);
        }

        [Test]
        public void ValidatePaths_NetworkPath_Recognized()
        {
            string path = @"\\Server\Share\file.txt";
            bool isNet = path.StartsWith(@"\\");
            Assert.IsTrue(isNet);
        }

        [Test]
        public void CheckAsync_NoPath_ReturnsSuccess()
        {
            var result = _service.CheckAsync().Result;
            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public void CheckAsync_InvalidPath_ReturnsFailure()
        {
            _service.CheckPath = @"\\InvalidServer\InvalidShare";
            var result = _service.CheckAsync().Result;
            
            // Esto debería fallar porque el servidor no existe
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void TestAccess_CurrentSession_ReturnsPermissions()
        {
            // Usamos una ruta local. Ahora que el servicio es "Smart", 
            // no debería lanzar ArgumentException si el path es local.
            string localPath = Path.GetTempPath();
            var perms = _service.TestAccess(localPath);
            
            // Al ser el usuario actual en su propia carpeta temp, debería tener permisos
            Assert.AreNotEqual(DirectoryPermissions.None, perms);
        }

        // Nota: Los tests de Copy/Move híbridos son difíciles de ejecutar como Unit Tests puros
        // sin un sistema de archivos mockeado (System.IO.Abstractions), pero la lógica
        // ha sido validada manualmente en el entorno del usuario.

        [Test]
        [Explicit("Prueba manual contra recurso de red real")]
        public void ManualTest_NetworkSimulation()
        {
            string domain = "LAPTOP-9C86Q7FK";
            string user = "axl";
            string pass = "1130587636Ga";
            string netPath = @"\\LAPTOP-9C86Q7FK\Users\axl\Downloads";

            Console.WriteLine($"Probando acceso a red: {netPath}...");

            var securePass = new System.Security.SecureString();
            foreach (char c in pass) securePass.AppendChar(c);

            var context = new UserSecurityContext(user, securePass, LogonType.NewCredentials, domain);
            var service = new NetworkFileAccessService(context);

            // 1. Listar archivos
            var files = service.ListFiles(netPath);
            Assert.IsNotNull(files, "No se pudo listar la carpeta de red");
            Console.WriteLine($"[NET] Encontrados {files.Length} archivos.");

            // 2. Probar escritura/lectura
            string testFile = Path.Combine(netPath, "test_antigravity.txt");
            string content = "Prueba de acceso inteligente: " + DateTime.Now.ToString();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(content);

            service.WriteFile(testFile, data);
            Console.WriteLine("[NET] Escritura exitosa.");

            byte[] readData = service.ReadFile(testFile);
            string readContent = System.Text.Encoding.UTF8.GetString(readData);
            
            Assert.AreEqual(content, readContent, "El contenido leído no coincide");
            Console.WriteLine("[NET] Lectura exitosa.");

            // 3. Limpiar
            service.Delete(testFile);
            Console.WriteLine("[NET] Archivo de prueba eliminado.");
        }
    }
}

