using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Axl.Base.Diagnostics.Tests
{
    // Minimal isolated MQTT peer: exercises the real M2Mqtt client over TCP and PUBACK.
    internal sealed class LoopbackMqttBroker : IDisposable
    {
        internal sealed class Publication { public string Topic; public byte[] Payload; public byte Header; }
        private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly object _gate = new object();
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly List<Publication> _publications = new List<Publication>();
        private readonly Dictionary<string, byte[]> _retained = new Dictionary<string, byte[]>();
        private volatile bool _stopped;
        private readonly bool _acknowledgePublish;
        public int Port { get; private set; }
        public LoopbackMqttBroker(bool acknowledgePublish = true)
        {
            _acknowledgePublish = acknowledgePublish;
            _listener.Start(); Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            new Thread(() => {
                while (!_stopped)
                    try
                    {
                        var client = _listener.AcceptTcpClient(); lock (_gate) _clients.Add(client);
                        new Thread(() => Serve(client)) { IsBackground = true }.Start();
                    }
                    catch (SocketException) { if (!_stopped) throw; }
            }) { IsBackground = true }.Start();
        }
        public Publication[] WaitForPublications(int count)
        {
            if (!SpinWait.SpinUntil(() => { lock (_gate) return _publications.Count >= count; }, 5000)) throw new TimeoutException("Local broker did not receive publication");
            lock (_gate) return _publications.ToArray();
        }
        private static byte[] ReadExactly(Stream stream, int length)
        {
            var result = new byte[length]; int offset = 0;
            while (offset < length) { var received = stream.Read(result, offset, length - offset); if (received == 0) throw new EndOfStreamException(); offset += received; }
            return result;
        }
        private static void Send(Stream stream, byte header, byte[] body)
        {
            stream.WriteByte(header); int remaining = body.Length;
            do { int next = remaining % 128; remaining /= 128; stream.WriteByte((byte)(next | (remaining > 0 ? 128 : 0))); } while (remaining > 0);
            stream.Write(body, 0, body.Length); stream.Flush();
        }
        private void Serve(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    while (!_stopped)
                    {
                        int header = stream.ReadByte(); if (header < 0) return;
                        int length = 0, multiplier = 1, octet;
                        do { octet = stream.ReadByte(); if (octet < 0) return; length += (octet & 127) * multiplier; multiplier *= 128; } while ((octet & 128) != 0);
                        var body = ReadExactly(stream, length);
                        if ((header >> 4) == 1) Send(stream, 0x20, new byte[] { 0, 0 });
                        else if ((header >> 4) == 3)
                        {
                            int topicLength = body[0] * 256 + body[1]; var topic = Encoding.UTF8.GetString(body, 2, topicLength);
                            int offset = 2 + topicLength; int qos = (header >> 1) & 3;
                            byte id1 = 0, id2 = 0; if (qos > 0) { id1 = body[offset++]; id2 = body[offset++]; }
                            var data = body.Skip(offset).ToArray();
                            lock (_gate) { _publications.Add(new Publication { Topic = topic, Payload = data, Header = (byte)header }); if ((header & 1) != 0) _retained[topic] = data; }
                            if (qos == 1 && _acknowledgePublish) Send(stream, 0x40, new[] { id1, id2 });
                        }
                        else if ((header >> 4) == 8)
                        {
                            int topicLength = body[2] * 256 + body[3]; var topic = Encoding.UTF8.GetString(body, 4, topicLength);
                            Send(stream, 0x90, new[] { body[0], body[1], (byte)0 });
                            byte[] data; lock (_gate) _retained.TryGetValue(topic, out data);
                            if (data != null)
                            {
                                var name = Encoding.UTF8.GetBytes(topic);
                                Send(stream, 0x31, new[] { (byte)(name.Length / 256), (byte)(name.Length % 256) }.Concat(name).Concat(data).ToArray());
                            }
                        }
                        else if ((header >> 4) == 12) Send(stream, 0xD0, new byte[0]);
                        else if ((header >> 4) == 14) return;
                    }
                }
            }
            catch (IOException) { } catch (ObjectDisposedException) { }
        }
        public void Dispose()
        {
            _stopped = true; _listener.Stop(); lock (_gate) foreach (var client in _clients) client.Close();
        }
    }
}
