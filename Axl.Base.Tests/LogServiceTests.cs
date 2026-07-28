using NUnit.Framework;
using Axl.Base.Logs;
using Axl.Base.Interfaces;
using Moq;
using System.IO;
using System;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class LogServiceTests
    {
        private string _testLogDir;

        [SetUp]
        public void SetUp()
        {
            _testLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestLogs");
            if (Directory.Exists(_testLogDir)) Directory.Delete(_testLogDir, true);
            Directory.CreateDirectory(_testLogDir);
        }

        [Test]
        public void FileLog_Write_CreatesFile()
        {
            var mockJson = new Mock<IJson>();
            var log = new FileLog(mockJson.Object, "production", _testLogDir);
            log.Info("Test message");
            
            string[] files = Directory.GetFiles(_testLogDir, "*.log");
            Assert.IsTrue(files.Length > 0);
        }

        [Test]
        public void ScreenLog_Write_DoesNotThrow()
        {
            var mockJson = new Mock<IJson>();
            var log = new ScreenLog(mockJson.Object);
            Assert.DoesNotThrow(() => log.Info("Test message"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testLogDir)) Directory.Delete(_testLogDir, true);
        }
    }
}

