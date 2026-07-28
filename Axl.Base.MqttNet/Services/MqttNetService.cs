using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Axl.Base.Models;
using Axl.Base.Interfaces;
using Axl.Base.Statics;
using System.IO;
using System.IO.Compression;

namespace Axl.Base.MqttNet.Services
{
    public class MqttNetService : IMqtt, IMqttService
    {
        private IMqttClient _mqttClient;
        private MqttClientOptions _mqttOptions;
        private readonly MqttFactory _factory;

        private readonly string _brokerUrl;
        private readonly int _port;
        private readonly string _clientId;

        private readonly bool _useTls;
        private readonly string _username;
        private readonly byte[] _passwordBytes;

        // LWT Fields
        private readonly string _lwtTopic;
        private readonly string _lwtPayload;
        private readonly bool _lwtRetain;
        private readonly MqttQualityOfServiceLevel _lwtQos;

        public event EventHandler<MqttData> DataReceived;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public bool IsConnected => _mqttClient != null && _mqttClient.IsConnected;
        public MqttQos Qos { get; set; } = MqttQos.AtLeastOnce;
        public bool UseCompression { get; set; }

        public MqttNetService(string brokerUrl, int port = 1883, string clientId = null, bool useTls = false, string username = null, string password = null, string lwtTopic = null, string lwtPayload = null, bool lwtRetain = false, MqttQualityOfServiceLevel lwtQos = MqttQualityOfServiceLevel.AtLeastOnce)
        {
            _brokerUrl = brokerUrl;
            _port = port;
            // Usamos MachineInfo de Axl.Base.Common
            _clientId = string.IsNullOrEmpty(clientId) ? MachineInfo.Hash(16) : clientId;
            _useTls = useTls;
            _username = username;

            if (!string.IsNullOrEmpty(password))
            {
                _passwordBytes = Encoding.UTF8.GetBytes(password);
            }

            _lwtTopic = lwtTopic;
            _lwtPayload = lwtPayload;
            _lwtRetain = lwtRetain;
            _lwtQos = lwtQos;

            _factory = new MqttFactory();
            _mqttClient = _factory.CreateMqttClient();
        }

        public async Task ConnectAsync()
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
                return;

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerUrl, _port)
                .WithClientId(_clientId)
                .WithCleanSession();

            if (!string.IsNullOrEmpty(_lwtTopic))
            {
                optionsBuilder.WithWillTopic(_lwtTopic)
                              .WithWillPayload(_lwtPayload)
                              .WithWillRetain(_lwtRetain)
                              .WithWillQualityOfServiceLevel(_lwtQos);
            }

            if (!string.IsNullOrEmpty(_username) && _passwordBytes != null && _passwordBytes.Length > 0)
            {
                string passwordString = Encoding.UTF8.GetString(_passwordBytes);
                optionsBuilder.WithCredentials(_username, passwordString);
            }

            if (_useTls)
            {
                optionsBuilder.WithTlsOptions(o =>
                {
                    o.UseTls();
                    o.WithCertificateValidationHandler(delegate { return true; });
                });
            }

            _mqttOptions = optionsBuilder.Build();

            _mqttClient.ConnectedAsync += OnConnected;
            _mqttClient.DisconnectedAsync += OnDisconnected;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

            try
            {
                await _mqttClient.ConnectAsync(_mqttOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"[ERROR(MQTT NET)] Failure in connection: {ex.Message}");
            }
        }

        public void Connect()
        {
            ConnectAsync().Wait();
        }

        private Task OnConnected(MqttClientConnectedEventArgs args)
        {
            Connected?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(0);
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs args)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(0);
        }

        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            byte[] rawPayload = e.ApplicationMessage.Payload;
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
                Topic = e.ApplicationMessage.Topic,
                Payload = payload,
                Timestamp = DateTime.Now
            };

            DataReceived?.Invoke(this, data);
            return Task.FromResult(0);
        }

        public async Task SubscribeAsync(string[] topics)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                return;

            var topicFilterBuilder = new MqttClientSubscribeOptionsBuilder();
            foreach (var topic in topics)
            {
                topicFilterBuilder.WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(MapQos(Qos)));
            }

            await _mqttClient.SubscribeAsync(topicFilterBuilder.Build());
        }

        public void Subscribe(string[] topics)
        {
            SubscribeAsync(topics).Wait();
        }

        public async Task PublishAsync(string topic, string payload)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                return;

            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            if (UseCompression)
            {
                payloadBytes = InternalCompress(payloadBytes);
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payloadBytes)
                .WithQualityOfServiceLevel(MapQos(Qos))
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.PublishAsync(message);
        }

        private MqttQualityOfServiceLevel MapQos(MqttQos qos)
        {
            switch (qos)
            {
                case MqttQos.AtMostOnce: return MqttQualityOfServiceLevel.AtMostOnce;
                case MqttQos.ExactlyOnce: return MqttQualityOfServiceLevel.ExactlyOnce;
                case MqttQos.AtLeastOnce:
                default: return MqttQualityOfServiceLevel.AtLeastOnce;
            }
        }

        public void Publish(string topic, string payload)
        {
            PublishAsync(topic, payload).Wait();
        }

        public async Task PublishAsync<T>(string topic, T payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            await PublishAsync(topic, json);
        }

        public void Publish<T>(string topic, T payload)
        {
            PublishAsync(topic, payload).Wait();
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

        public async Task DisconnectAsync()
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
            }
        }

        public void Disconnect()
        {
            DisconnectAsync().Wait();
        }

        public void Dispose()
        {
            Disconnect();
            _mqttClient?.Dispose();
        }
    }
}
