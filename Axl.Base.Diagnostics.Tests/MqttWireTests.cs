using System;
using System.Text;
using System.Threading;
using Axl.Base.Models;
using Axl.Base.Mqtt.Services;
using NUnit.Framework;

namespace Axl.Base.Diagnostics.Tests
{
    [TestFixture]
    public class MqttWireTests
    {
        [Test, Timeout(20000)] public void StatusIsPlainRetainedQos1_WhileExistingPublishesKeepCompressionAndQos()
        {
            using (var broker = new LoopbackMqttBroker())
            using (var publisher = new MqttService("127.0.0.1", broker.Port, "diagnostics-publisher"))
            {
                publisher.Qos = MqttQos.AtMostOnce; publisher.UseCompression = true; publisher.Connect();
                var json = "{\"status\":\"online\",\"collector_version\":\"1.1.1.0\"}";
                publisher.Publish("otms/test/status", json, MqttQos.AtLeastOnce, true, false);
                publisher.Publish("otms/test/icmp", "{\"value\":1}");
                var packets = broker.WaitForPublications(2);
                Assert.That(Encoding.UTF8.GetString(packets[0].Payload), Is.EqualTo(json));
                Assert.That(packets[0].Header & 7, Is.EqualTo(3)); // QoS1 + retained
                Assert.That(packets[1].Header & 7, Is.Zero);
                Assert.That(packets[1].Payload[0], Is.EqualTo(0x1f)); Assert.That(packets[1].Payload[1], Is.EqualTo(0x8b));
                Assert.That(publisher.Qos, Is.EqualTo(MqttQos.AtMostOnce)); Assert.That(publisher.UseCompression, Is.True);
                publisher.Disconnect();
                using (var subscriber = new MqttService("127.0.0.1", broker.Port, "late-status-reader"))
                using (var received = new ManualResetEvent(false))
                {
                    string payload = null; subscriber.DataReceived += (sender, data) => { payload = data.Payload; received.Set(); };
                    subscriber.Connect(); subscriber.Subscribe(new[] { "otms/test/status" });
                    Assert.That(received.WaitOne(5000), Is.True); Assert.That(payload, Is.EqualTo(json));
                }
            }
        }

        [Test, Timeout(15000)] public void ControlledOfflineWaitsForPubackBeforeDisconnect()
        {
            using (var broker = new LoopbackMqttBroker())
            using (var publisher = new MqttService("127.0.0.1", broker.Port, "offline-publisher"))
            {
                publisher.Connect(); Assert.That(publisher.PublishConfirmed("otms/test/status", "{\"status\":\"offline\"}"), Is.True);
                publisher.Disconnect(); var packet = broker.WaitForPublications(1)[0];
                Assert.That(packet.Header & 7, Is.EqualTo(3)); Assert.That(Encoding.UTF8.GetString(packet.Payload), Does.Contain("offline"));
            }
        }
    }
}
