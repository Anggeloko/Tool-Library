using NUnit.Framework;
using Moq;
using System;
using System.IO;
using System.Collections.Generic;
using Axl.Base.Interfaces;
using Axl.Base.Json.Legacy;
using Axl.Base.Persistence.Services;
using Axl.Base.Persistence.Interfaces;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class PersistenceTests
    {
        private string _testPath;
        private IJson _json;
        private Mock<ILog> _mockLogger;

        public class SampleData
        {
            public int Id { get; set; }
            public string Value { get; set; }
            public List<string> Tags { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            _testPath = Path.Combine(Path.GetTempPath(), "AxlPersistenceTests_" + Guid.NewGuid().ToString("N"));
            if (!Directory.Exists(_testPath)) Directory.CreateDirectory(_testPath);
            
            _json = new LegacyJson();
            _mockLogger = new Mock<ILog>();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testPath))
            {
                try { Directory.Delete(_testPath, true); } catch { }
            }
        }

        [Test]
        public void SaveAndLoad_ShouldRestoreObjectCorrectly()
        {
            // Arrange
            var storage = new LocalVariableStorage(_testPath, _json, _mockLogger.Object, cleanupDays: 0);
            var originalData = new SampleData 
            { 
                Id = 123, 
                Value = "Test Persistence", 
                Tags = new List<string> { "tag1", "tag2" } 
            };
            string key = "TEST_VAR";

            // Act
            storage.Save(key, originalData);
            var loadedData = storage.Load<SampleData>(key);

            // Assert
            Assert.IsNotNull(loadedData);
            Assert.AreEqual(originalData.Id, loadedData.Id);
            Assert.AreEqual(originalData.Value, loadedData.Value);
            Assert.AreEqual(originalData.Tags.Count, loadedData.Tags.Count);
            Assert.AreEqual(originalData.Tags[0], loadedData.Tags[0]);
        }

        [Test]
        public void Load_NonExistentKey_ShouldReturnDefault()
        {
            // Arrange
            var storage = new LocalVariableStorage(_testPath, _json, _mockLogger.Object, cleanupDays: 0);

            // Act
            var result = storage.Load<SampleData>("NON_EXISTENT");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void Delete_ShouldRemoveFile()
        {
            // Arrange
            var storage = new LocalVariableStorage(_testPath, _json, _mockLogger.Object, cleanupDays: 0);
            var data = new SampleData { Id = 1 };
            string key = "TO_DELETE";
            storage.Save(key, data);
            
            string filePath = Path.Combine(_testPath, "Data", key + ".bin");
            Assert.IsTrue(File.Exists(filePath), "File should exist after Save");

            // Act
            storage.Delete(key);

            // Assert
            Assert.IsFalse(File.Exists(filePath), "File should be deleted");
            var result = storage.Load<SampleData>(key);
            Assert.IsNull(result);
        }

        [Test]
        public void Cleanup_WhenEnabled_ShouldDeleteOldFiles()
        {
            // Arrange
            var storage = new LocalVariableStorage(_testPath, _json, _mockLogger.Object, cleanupDays: 1);
            string dataDir = Path.Combine(_testPath, "Data");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

            // Create a fake old file manually
            string oldFile = Path.Combine(dataDir, "OLD_FILE.bin");
            File.WriteAllText(oldFile, "Old content");
            File.SetCreationTime(oldFile, DateTime.Now.AddDays(-2));

            // Create a fresh variable
            storage.Save("NEW_VAR", new SampleData { Id = 1 });

            // Assert
            Assert.IsFalse(File.Exists(oldFile), "Old file should have been cleaned up");
            Assert.IsTrue(File.Exists(Path.Combine(dataDir, "NEW_VAR.bin")), "New file should still exist");
        }
    }
}
