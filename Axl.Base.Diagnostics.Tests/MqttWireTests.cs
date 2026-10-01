using System;
using System.IO;
using System.IO.Compression;
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
        [TestCase(false), TestCase(true), Timeout(20000)]
        public void StatusSupportsPerMessageCompressionAndRetainedQos1_WhileExistingPublishesKeepOptions(bool compress)
        {
            using (var broker = new LoopbackMqttBroker())
            using (var publisher = new MqttService("127.0.0.1", broker.Port, "diagnostics-publisher"))
            {
                publisher.Qos = MqttQos.AtMostOnce; publisher.UseCompression = true; publisher.Connect();
                var json = "{\"status\":\"online\",\"collector_version\":\"1.1.1.0\"}";
                publisher.Publish("otms/test/status", json, MqttQos.AtLeastOnce, true, compress);
                publisher.Publish("otms/test/icmp", "{\"value\":1}");
                var packets = broker.WaitForPublications(2);
                Assert.That(ReadPayload(packets[0].Payload, compress), Is.EqualTo(json));
                Assert.That(packets[0].Header & 7, Is.EqualTo(3)); // QoS1 + retained
                Assert.That(packets[1].Header & 7, Is.Zero);
                Assert.That(packets[1].Payload[0], Is.EqualTo(0x1f)); Assert.That(packets[1].Payload[1], Is.EqualTo(0x8b));
                Assert.That(publisher.Qos, Is.EqualTo(MqttQos.AtMostOnce)); Assert.That(publisher.UseCompression, Is.True);
                publisher.Disconnect();
                using (var subscriber = new MqttService("127.0.0.1", broker.Port, "late-status-reader"))
                using (var received = new ManualResetEvent(false))
                {
                    subscriber.UseCompression = compress;
                    string payload = null; subscriber.DataReceived += (sender, data) => { payload = data.Payload; received.Set(); };
                    subscriber.Connect(); subscriber.Subscribe(new[] { "otms/test/status" });
                    Assert.That(received.WaitOne(5000), Is.True); Assert.That(payload, Is.EqualTo(json));
                }
            }
        }

        [TestCase(false), TestCase(true), Timeout(15000)]
        public void ControlledOfflineWaitsForPubackBeforeDisconnect(bool compress)
        {
            using (var broker = new LoopbackMqttBroker())
            using (var publisher = new MqttService("127.0.0.1", broker.Port, "offline-publisher"))
            {
                var json = "{\"status\":\"offline\"}";
                publisher.Connect();
                var confirmed = compress
                    ? publisher.PublishConfirmed("otms/test/status", json, true)
                    : publisher.PublishConfirmed("otms/test/status", json);
                Assert.That(confirmed, Is.True);
                publisher.Disconnect(); var packet = broker.WaitForPublications(1)[0];
                Assert.That(packet.Header & 7, Is.EqualTo(3));
                Assert.That(ReadPayload(packet.Payload, compress), Is.EqualTo(json));
                using (var subscriber = new MqttService("127.0.0.1", broker.Port, "late-offline-reader"))
                using (var received = new ManualResetEvent(false))
                {
                    subscriber.UseCompression = compress;
                    string payload = null;
                    subscriber.DataReceived += (sender, data) => { payload = data.Payload; received.Set(); };
                    subscriber.Connect(); subscriber.Subscribe(new[] { "otms/test/status" });
                    Assert.That(received.WaitOne(5000), Is.True);
                    Assert.That(payload, Is.EqualTo(json));
                }
            }
        }

        private static string ReadPayload(byte[] payload, bool compressed)
        {
            if (!compressed) return Encoding.UTF8.GetString(payload);
            Assert.That(payload[0], Is.EqualTo(0x1f));
            Assert.That(payload[1], Is.EqualTo(0x8b));
            using (var input = new MemoryStream(payload))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzip, Encoding.UTF8)) return reader.ReadToEnd();
        }
    }
}
