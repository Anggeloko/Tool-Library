using NUnit.Framework;
using Moq;
using Axl.Base.Sftp.Services;
using Axl.Base.Sftp.Models;
using Axl.Base.Interfaces;
using System.Threading.Tasks;
using System;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class SftpServiceTests
    {
        [Test]
        public void Constructor_ShouldThrow_WhenConfigIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SftpService(null));
        }

        [Test]
        public async Task CheckAsync_ShouldReturnFailure_WhenConnectionFails()
        {
            // Arrange
            var config = new SftpConfig { Host = "invalid_host" };
            var mockLog = new Mock<ILog>();
            var service = new SftpService(config, mockLog.Object);

            // Act
            var result = await service.CheckAsync();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            mockLog.Verify(l => l.Error(It.Is<string>(s => s.Contains("Check failed")), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task UploadFile_ShouldReturnFailure_WhenLocalFileDoesNotExist()
        {
            // Arrange
            var config = new SftpConfig { Host = "localhost" };
            var service = new SftpService(config);

            // Act
            var result = await service.UploadFile("non_existent_file.txt", "remote");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Error.Contains("does not exist"));
        }

        [Test]
        [Explicit("Prueba manual contra servidor SFTP real")]
        public async Task ManualTest_SftpSimulation()
        {
            var config = new SftpConfig
            {
                Host = "127.0.0.1",
                Port = 2222,
                User = "axl",
                Password = "16845008Af",
                RemotePath = "/upload"
            };

            Console.WriteLine($"Conectando a SFTP {config.Host}:{config.Port}...");
            var service = new SftpService(config);
            
            var checkResult = await service.CheckAsync();
            Assert.IsTrue(checkResult.IsSuccess, "La conexión SFTP falló: " + checkResult.Error);
            Console.WriteLine("¡Conexión SFTP exitosa!");
        }
    }
}

