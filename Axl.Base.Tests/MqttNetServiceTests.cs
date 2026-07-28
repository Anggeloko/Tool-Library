using NUnit.Framework;
using Axl.Base.MqttNet.Services;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using System;
using System.Threading.Tasks;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class MqttNetServiceTests
    {
        private string _brokerUrl = "localhost";

        [Test]
        public void Constructor_ValidParams_ShouldInitialize()
        {
            var service = new MqttNetService(_brokerUrl);
            Assert.IsNotNull(service);
        }

        [Test]
        public async Task DisconnectAsync_WhenNotConnected_ShouldNotThrow()
        {
            var service = new MqttNetService(_brokerUrl);
            await service.DisconnectAsync();
            Assert.Pass();
        }

        [Test]
        public void Dispose_ShouldHandleRepetitiveCalls()
        {
            var service = new MqttNetService(_brokerUrl);
            service.Dispose();
            Assert.DoesNotThrow(() => service.Dispose());
        }

        [Test]
        public void IsConnected_ShouldBeFalseInitially()
        {
            var service = new MqttNetService(_brokerUrl);
            Assert.IsFalse(service.IsConnected);
        }

        [Test]
        public void Events_ShouldAllowSubscription()
        {
            var service = new MqttNetService(_brokerUrl);
            bool connectedCalled = false;
            bool disconnectedCalled = false;

            service.Connected += (s, e) => connectedCalled = true;
            service.Disconnected += (s, e) => disconnectedCalled = true;

            Assert.IsFalse(connectedCalled);
            Assert.IsFalse(disconnectedCalled);
        }
    }
}

