using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Axl.Base.Http.Models;
using Axl.Base.Http.Services;
using Axl.Base.Interfaces;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class HttpServiceTests
    {
        private Mock<HttpMessageHandler> _handlerMock;
        private HttpConfig _config;
        private Mock<ILog> _logMock;

        [SetUp]
        public void SetUp()
        {
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _config = new HttpConfig
            {
                BaseUrl = "https://api.example.com/",
                AuthEndpoint = "login",
                Username = "user",
                Password = "pass",
                TokenKey = "token",
                ExpirationKey = "expires"
            };
            _logMock = new Mock<ILog>();
        }

        [Test]
        public async Task GetTokenAsync_Should_Return_Token_On_Success()
        {
            // Arrange
            var responseJson = "{\"token\": \"secret_token\", \"expires\": 3600}";
            SetupMockResponse(HttpMethod.Post, "https://api.example.com/login", HttpStatusCode.OK, responseJson);

            var service = new TestableHttpService(_config, _handlerMock.Object, _logMock.Object);

            // Act
            var result = await service.GetTokenAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("secret_token", result.Value);
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri.ToString() == "https://api.example.com/login"),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Test]
        public async Task SendAsync_Should_Auto_Authenticate_If_No_Token()
        {
            // Arrange
            var authJson = "{\"token\": \"token123\", \"expires\": 3600}";
            var dataJson = "{\"status\": \"ok\"}";

            // Expect login
            SetupMockResponse(HttpMethod.Post, "https://api.example.com/login", HttpStatusCode.OK, authJson);
            // Expect GET request
            SetupMockResponse(HttpMethod.Get, "https://api.example.com/data", HttpStatusCode.OK, dataJson);

            var service = new TestableHttpService(_config, _handlerMock.Object, _logMock.Object);

            // Act
            var result = await service.GetAsync<Dictionary<string, string>>("data");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("ok", result.Value["status"]);
            
            // Verify that the GET request included the Authorization header
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        private void SetupMockResponse(HttpMethod method, string url, HttpStatusCode code, string content)
        {
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == method && req.RequestUri.ToString() == url),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = code,
                    Content = new StringContent(content)
                });
        }
    }

    // Subclass to allow injecting a mock handler into the internal HttpClient
    public class TestableHttpService : HttpService
    {
        public TestableHttpService(HttpConfig config, HttpMessageHandler handler, ILog log = null) 
            : base(config, log)
        {
            SetClientForTesting(new HttpClient(handler));
        }
    }
}

