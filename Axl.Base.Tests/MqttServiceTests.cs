using System;
using NUnit.Framework;
using Moq;
using Axl.Base.Mqtt.Services;
using Axl.Base.Models;
using System.Text;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class MqttServiceTests
    {
        private MqttService _mqttService;
        private string _brokerUrl = "localhost";

        [SetUp]
        public void SetUp()
        {
            _mqttService = new MqttService(_brokerUrl);
        }

        [Test]
        public void Constructor_ShouldInitializeProperties()
        {
            Assert.IsNotNull(_mqttService);
        }

        [Test]
        public void Publish_WhenNotConnected_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => _mqttService.Publish("test/topic", "test payload"));
        }

        [Test]
        public void Dispose_ShouldHandleRepetitiveCalls()
        {
            _mqttService.Dispose();
            Assert.DoesNotThrow(() => _mqttService.Dispose());
        }

        [Test]
        public void IsConnected_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_mqttService.IsConnected);
        }

        [Test]
        public void Events_ShouldAllowSubscription()
        {
            bool connectedCalled = false;
            bool disconnectedCalled = false;

            _mqttService.Connected += (s, e) => connectedCalled = true;
            _mqttService.Disconnected += (s, e) => disconnectedCalled = true;

            Assert.IsFalse(connectedCalled);
            Assert.IsFalse(disconnectedCalled);
        }
    }
}
