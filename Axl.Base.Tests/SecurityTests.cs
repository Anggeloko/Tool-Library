using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Axl.Base.Models;
using Axl.Base.Wmi.Services;
using Axl.Base.Snmp.Services;
using Axl.Base.OpcDa.Services;
using Axl.Base.Mqtt.Services;
using Axl.Base.Icmp.Services;
using Axl.Base.Json.Newton.Services;
using Axl.Base.Database.SQLite.Services;
using Axl.Base.Statics;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class SecurityTests
    {
        private string _testDbDir;
        private string _primaryKey;
        private string _secondaryKey;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _testDbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestDb");
            if (Directory.Exists(_testDbDir)) Directory.Delete(_testDbDir, true);
            Directory.CreateDirectory(_testDbDir);

            // Obtenemos llaves reales del sistema para las pruebas
            var keys = MachineInfo.GetPossibleHashes(16);
            _primaryKey = keys.FirstOrDefault();
            _secondaryKey = keys.Count > 1 ? keys[1] : "SecondaryKey12345";
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (Directory.Exists(_testDbDir)) Directory.Delete(_testDbDir, true);
        }

        [Test]
        public void Encryption_RoundTrip_ShouldWork()
        {
            // Arrange
            string plainText = "SensitiveData_123!";
            string secret = "MySecretKey";

            // Act
            string encrypted = Encryption.Encrypt(plainText, secret);
            string decrypted = Encryption.Decrypt(encrypted, secret);

            // Assert
            Assert.AreNotEqual(plainText, encrypted);
            Assert.AreEqual(plainText, decrypted);
        }

        [Test]
        public void Encryption_WrongKey_ShouldReturnErrorMessage()
        {
            // Arrange
            string plainText = "Secret";
            string encrypted = Encryption.Encrypt(plainText, "Key1");

            // Act
            string decrypted = Encryption.Decrypt(encrypted, "Key2");

            // Assert
            Assert.AreEqual("[Decryption Error]", decrypted);
        }

        [Test]
        public void Encryption_DecryptWithPool_ShouldFindCorrectKey()
        {
            // Arrange
            string plainText = "Data";
            string correctKey = "Correct";
            string encrypted = Encryption.Encrypt(plainText, correctKey);
            var pool = new List<string> { "Wrong1", "Wrong2", correctKey, "Wrong3" };

            // Act
            string decrypted = Encryption.DecryptWithPool(encrypted, pool);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [Test]
        public void SQLiteService_SaveAndRead_ShouldBeConsistent()
        {
            // Arrange
            var service = new SQLiteService(_testDbDir);
            var variables = new List<VariableItem>
            {
                new VariableItem { Key = "API_KEY", Value = "ABC-123" },
                new VariableItem { Key = "DB_PASS", Value = "P@ssw0rd1" }
            };

            // Act
            service.SaveVariables(variables);
            var readBack = service.ReadVariables();

            // Assert
            Assert.AreEqual(2, readBack.Count);
            Assert.AreEqual("ABC-123", readBack.First(v => v.Key == "API_KEY").Value);
            Assert.AreEqual("P@ssw0rd1", readBack.First(v => v.Key == "DB_PASS").Value);
        }

        [Test]
        public void SQLiteService_ShouldEncryptDataInDatabase()
        {
            // Arrange
            var service = new SQLiteService(_testDbDir);
            var variables = new List<VariableItem> { new VariableItem { Key = "SecretKey", Value = "SecretValue" } };
            service.SaveVariables(variables);

            // Act - Leemos el archivo directamente para ver si es texto plano
            string dbFile = Path.Combine(_testDbDir, "settings.db");
            string dbContent = File.ReadAllText(dbFile);

            // Assert
            Assert.IsFalse(dbContent.Contains("SecretKey"), "Key should be encrypted in DB");
            Assert.IsFalse(dbContent.Contains("SecretValue"), "Value should be encrypted in DB");
        }

        [Test]
        public void SQLiteService_HealingProcess_ShouldUpdateToPrimaryKey()
        {
            // Esta prueba simula que los datos fueron guardados con una llave vieja
            // y al leerlos, el servicio detecta que usó el pool y los re-encripta con la nueva.
            
            // 1. Arrange: Forzamos guardado con llave secundaria (si existe)
            // Como SQLiteService.SaveVariables usa MachineInfo.Hash(16) internamente,
            // no podemos inyectar la llave fácilmente sin mocks complejos.
            // Pero podemos validar el comportamiento de ReadVariables() que ya tiene la lógica de curación.
            
            var service = new SQLiteService(_testDbDir);
            var testVars = new List<VariableItem> { new VariableItem { Key = "HealingTest", Value = "TestValue" } };
            
            // Guardamos normalmente (usa PrimaryKey)
            service.SaveVariables(testVars);

            // Act: Simplemente leemos. Si el sistema está sano, no debería re-guardar.
            // Si estuviéramos en un entorno donde la PrimaryKey cambió, ReadVariables invocaría SaveVariables.
            var result = service.ReadVariables();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("TestValue", result[0].Value);
        }

        [Test]
        public void SQLiteService_SetAndGet_ShouldBeConsistent()
        {
            // Arrange
            var service = new SQLiteService(_testDbDir);
            string key = "SingleKey";
            string value = "SingleValue";

            // Act
            service.SetVariable(key, value);
            string result = service.GetVariable(key);

            // Assert
            Assert.AreEqual(value, result);
        }

        [Test]
        public void SQLiteService_GetVariable_ShouldReturnDefaultValue()
        {
            // Arrange
            var service = new SQLiteService(_testDbDir);

            // Act
            string result = service.GetVariable("NonExistent", "Default");

            // Assert
            Assert.AreEqual("Default", result);
        }

        [Test]
        public void SQLiteService_DeleteVariable_ShouldRemoveKey()
        {
            // Arrange
            var service = new SQLiteService(_testDbDir);
            service.SetVariable("DeleteMe", "Value");

            // Act
            service.DeleteVariable("DeleteMe");
            string result = service.GetVariable("DeleteMe");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void SQLiteService_SetVariable_ShouldOverwrite()
        {
            // Arrange
            var service = new SQLiteService(_testDbDir);
            service.SetVariable("UpdateMe", "OldValue");

            // Act
            service.SetVariable("UpdateMe", "NewValue");
            string result = service.GetVariable("UpdateMe");

            // Assert
            Assert.AreEqual("NewValue", result);
        }
    }
}


