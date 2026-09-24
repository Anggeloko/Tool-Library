using System;
using System.Net;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Axl.Base.Models;
using Axl.Base.Interfaces;
using System.IO;
using System.IO.Compression;

namespace Axl.Base.Mqtt.Services
{
    public class MqttService : IMqttService
    {
        private MqttClient _mqttClient;
        private readonly string _brokerUrl;
        private readonly int _port;
        private readonly string _clientId;
        private readonly bool _useTls;
        private readonly string _username;
        private readonly string _password;

        // LWT Fields
        private readonly string _lwtTopic;
        private readonly string _lwtPayload;
        private readonly bool _lwtRetain;
        private readonly byte _lwtQos;

        public event EventHandler<MqttData> DataReceived;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public bool IsConnected => _mqttClient != null && _mqttClient.IsConnected;
        public MqttQos Qos { get; set; } = MqttQos.AtLeastOnce;
        public bool UseCompression { get; set; }

        public MqttService(string brokerUrl, int port = 1883, string clientId = "OpcUaToSqlConnectorMqttClient", bool useTls = false, string username = null, string password = null, string lwtTopic = null, string lwtPayload = null, bool lwtRetain = false)
        {
            _brokerUrl = brokerUrl;
            _port = port;
            _clientId = clientId;
            _useTls = useTls;
            _username = username;
            _password = password;

            _lwtTopic = lwtTopic;
            _lwtPayload = lwtPayload;
            _lwtRetain = lwtRetain;
            _lwtQos = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;

            if (IPAddress.TryParse(brokerUrl, out IPAddress ipAddress))
            {
                _brokerIpAddress = ipAddress;
            }

            InitializeClient();
        }

        private IPAddress _brokerIpAddress;

        private void InitializeClient()
        {
            if (_mqttClient != null)
            {
                try
                {
                    _mqttClient.MqttMsgPublishReceived -= OnMessageReceived;
                    _mqttClient.MqttMsgDisconnected -= OnConnectionClosed;
                    if (_mqttClient.IsConnected) _mqttClient.Disconnect();
                }
                catch { }
            }

            if (_brokerIpAddress != null)
            {
                _mqttClient = new MqttClient(_brokerIpAddress, _port, _useTls, null);
            }
            else
            {
                _mqttClient = new MqttClient(_brokerUrl, _port, _useTls, null);
            }

            _mqttClient.MqttMsgPublishReceived += OnMessageReceived;
            _mqttClient.MqttMsgDisconnected += OnConnectionClosed;
        }

        public void Connect()
        {
            if (IsConnected) return;

            try
            {
                // Recreate the legacy client on the reconnecting thread, rather than
                // replacing it inside M2Mqtt's disconnection callback.
                InitializeClient();
                if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                {
                    if (!string.IsNullOrEmpty(_lwtTopic))
                    {
                        _mqttClient.Connect(_clientId, _username, _password, _lwtRetain, _lwtQos, true, _lwtTopic, _lwtPayload, true, 60);
                    }
                    else
                    {
                        _mqttClient.Connect(_clientId, _username, _password);
                    }
                }
                else
                {
                    _mqttClient.Connect(_clientId);
                }

                if (IsConnected)
                {
                    Connected?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"[ERROR(MQTT)] Failure in connection: {ex.Message}");
            }
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            Console.WriteLine("[MQTT] Connection closed.");
            Disconnected?.Invoke(this, EventArgs.Empty);
            
            // The next Connect recreates the client after all in-flight callbacks finish.
        }

        private void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
        {
            byte[] rawPayload = e.Message;
            string payload = string.Empty;

            if (UseCompression && rawPayload != null && rawPayload.Length > 0)
            {
                try
                {
                    rawPayload = InternalDecompress(rawPayload);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MQTT] Internal decompression failed: {ex.Message}");
                }
            }

            if (rawPayload != null)
            {
                payload = Encoding.UTF8.GetString(rawPayload);
            }

            var data = new MqttData
            {
                Topic = e.Topic,
                Payload = payload,
                Timestamp = DateTime.Now
            };

            DataReceived?.Invoke(this, data);
        }

        public void Subscribe(string[] topics)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MQTT] Attempting subscribe without connection.");
                return;
            }

            byte qosLevel = MapQos(Qos);
            byte[] qosLevels = new byte[topics.Length];
            for (int i = 0; i < topics.Length; i++)
            {
                qosLevels[i] = qosLevel;
            }

            _mqttClient.Subscribe(topics, qosLevels);
        }

        public void Publish(string topic, string payload)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MQTT] Attempting publish without connection.");
                return;
            }

            var message = Encoding.UTF8.GetBytes(payload);

            if (UseCompression)
            {
                message = InternalCompress(message);
            }

            _mqttClient.Publish(topic, message, MapQos(Qos), false);
        }

        private byte MapQos(MqttQos qos)
        {
            switch (qos)
            {
                case MqttQos.AtMostOnce: return MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
                case MqttQos.ExactlyOnce: return MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE;
                case MqttQos.AtLeastOnce:
                default: return MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;
            }
        }

        public void Publish<T>(string topic, T payload)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            Publish(topic, json);
        }

        private byte[] InternalCompress(byte[] data)
        {
            if (data == null || data.Length == 0) return data;
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }

        private byte[] InternalDecompress(byte[] data)
        {
            if (data == null || data.Length == 0) return data;
            using (var ms = new MemoryStream(data))
            using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
            using (var outMs = new MemoryStream())
            {
                gzip.CopyTo(outMs);
                return outMs.ToArray();
            }
        }

        public void Disconnect()
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                _mqttClient.Disconnect();
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
