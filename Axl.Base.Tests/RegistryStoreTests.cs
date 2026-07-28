using System;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32;
using NUnit.Framework;
using Axl.Base.RegistryStore.Services;
using Axl.Base.Models;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class RegistryStoreTests
    {
        private const string TestRegistryPath = @"SOFTWARE\AxlBaseTests\Store";
        private RegistryKeyStore _store;

        [SetUp]
        public void Setup()
        {
            _store = new RegistryKeyStore(TestRegistryPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (IsRunningAsAdmin())
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        baseKey.DeleteSubKeyTree(@"SOFTWARE\AxlBaseTests", false);
                    }
                }
                catch { /* Ignorar errores en limpieza */ }
            }
        }

        private bool IsRunningAsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        [Test]
        public void StoreAndRetrieveSecret_ShouldWork_WhenAdmin()
        {
            if (!IsRunningAsAdmin())
            {
                Assert.Ignore("Esta prueba requiere privilegios de Administrador para escribir en HKLM.");
                return;
            }

            // Arrange
            string secretKey = "MyTestSecret";
            string secretValue = "SuperSecurePassword123!";

            // Act
            _store.StoreSecret(secretKey, secretValue);
            string retrievedValue = _store.RetrieveSecret(secretKey);

            // Assert
            Assert.AreEqual(secretValue, retrievedValue);
        }

        [Test]
        public void StoreAndRetrieveSecretValue_ShouldWork_WhenAdmin()
        {
            if (!IsRunningAsAdmin())
            {
                Assert.Ignore("Esta prueba requiere privilegios de Administrador para escribir en HKLM.");
                return;
            }

            // Arrange
            string secretKey = "SecretValueSecret";
            string secretValue = "AnotherSecretValue_456!";
            
            using (var secretVal = new SecretValue(secretValue))
            {
                // Act
                _store.StoreSecret(secretKey, secretVal);
            }

            using (SecretValue retrievedSecret = _store.RetrieveSecretSecure(secretKey))
            {
                // Assert
                Assert.IsNotNull(retrievedSecret);
                Assert.AreEqual(secretValue, retrievedSecret.GetString());
            }
        }

        [Test]
        public void DeleteSecret_ShouldRemoveKey_WhenAdmin()
        {
            if (!IsRunningAsAdmin())
            {
                Assert.Ignore("Esta prueba requiere privilegios de Administrador.");
                return;
            }

            // Arrange
            string secretKey = "DeleteTestSecret";
            string secretValue = "TempSecret";

            _store.StoreSecret(secretKey, secretValue);
            Assert.AreEqual(secretValue, _store.RetrieveSecret(secretKey));

            // Act
            _store.DeleteSecret(secretKey);
            string retrievedValue = _store.RetrieveSecret(secretKey);

            // Assert
            Assert.IsNull(retrievedValue);
        }

        [Test]
        public void RetrieveNonExistentSecret_ShouldReturnNull()
        {
            if (!IsRunningAsAdmin())
            {
                Assert.Ignore("Esta prueba requiere privilegios de Administrador.");
                return;
            }

            // Act
            string retrieved = _store.RetrieveSecret("NonExistentKey123");

            // Assert
            Assert.IsNull(retrieved);
        }

        [Test]
        public void Constructor_EmptyRegistryPath_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new RegistryKeyStore(""));
            Assert.Throws<ArgumentException>(() => new RegistryKeyStore(null));
        }

        [Test]
        public void SecretValue_Dispose_ShouldZeroMemory()
        {
            // Arrange
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes("SensitiveData");
            var secretVal = new SecretValue(bytes);

            // Act
            secretVal.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => secretVal.GetBytes());
            Assert.Throws<ObjectDisposedException>(() => secretVal.GetString());
        }

        [Test]
        public void DeleteStoreTree_ShouldRemoveAllKeysAndSubkeys_WhenAdmin()
        {
            if (!IsRunningAsAdmin())
            {
                Assert.Ignore("Esta prueba requiere privilegios de Administrador.");
                return;
            }

            // Arrange
            _store.StoreSecret("SubKey1", "Value1");
            _store.StoreSecret("SubKey2", "Value2");

            // Act
            _store.DeleteStoreTree();

            // Assert
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = baseKey.OpenSubKey(TestRegistryPath))
            {
                Assert.IsNull(key); // The entire folder should be deleted
            }
        }

        [Test]
        public void DeleteStoreTree_ShouldThrowSecurityException_WhenPathIsTooShort()
        {
            // Arrange
            var insecureStore1 = new RegistryKeyStore(@"SOFTWARE");
            var insecureStore2 = new RegistryKeyStore(@"SOFTWARE\SchneiderElectric");

            // Act & Assert
            Assert.Throws<SecurityException>(() => insecureStore1.DeleteStoreTree());
            Assert.Throws<SecurityException>(() => insecureStore2.DeleteStoreTree());
        }
    }
}
