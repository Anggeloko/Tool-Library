using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Orchestrator.Mqtt.Ports;

namespace Axl.Base.Orchestrator.Mqtt.Services
{
    public class MqttMessageOrchestrator : IDisposable
    {
        private readonly IMqttService _mqttService;
        private readonly List<IMqttTopicHandler> _handlers;
        private readonly ILog _log;

        public MqttMessageOrchestrator(
            IMqttService mqttService,
            IEnumerable<IMqttTopicHandler> handlers,
            ILog log = null)
        {
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
            _handlers = handlers != null ? handlers.ToList() : new List<IMqttTopicHandler>();
            _log = log;

            AttachEvents();
        }

        private void AttachEvents()
        {
            _mqttService.Connected += OnMqttConnected;
            _mqttService.Disconnected += OnMqttDisconnected;
            _mqttService.DataReceived += OnMqttDataReceived;
        }

        private void OnMqttConnected(object sender, EventArgs e)
        {
            _log?.Info("[MqttMessageOrchestrator] MQTT client connected. Subscribing registered topics...");
            SubscribeAllTopics();
        }

        private void OnMqttDisconnected(object sender, EventArgs e)
        {
            _log?.Warn("[MqttMessageOrchestrator] MQTT client disconnected. Standby for automatic reconnection...");
        }

        public void RegisterHandler(IMqttTopicHandler handler)
        {
            if (handler == null) return;
            lock (_handlers)
            {
                if (!_handlers.Contains(handler))
                {
                    _handlers.Add(handler);
                    _log?.Info($"[MqttMessageOrchestrator] Dynamic MQTT topic handler registered for '{handler.SubscriptionTopic}'.");
                }
            }
        }

        public void RegisterAndSubscribeHandler(IMqttTopicHandler handler)
        {
            RegisterHandler(handler);

            if (_mqttService.IsConnected && !string.IsNullOrEmpty(handler.SubscriptionTopic))
            {
                _log?.Info($"[MqttMessageOrchestrator] Dynamic subscription to topic '{handler.SubscriptionTopic}'...");
                _mqttService.Subscribe(new[] { handler.SubscriptionTopic });
            }
        }

        public void SubscribeAllTopics()
        {
            if (!_mqttService.IsConnected)
            {
                _log?.Warn("[MqttMessageOrchestrator] Cannot subscribe: MQTT client is not connected.");
                return;
            }

            string[] topics;
            lock (_handlers)
            {
                topics = _handlers
                    .Select(h => h.SubscriptionTopic)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .ToArray();
            }

            if (topics.Length > 0)
            {
                _log?.Info($"[MqttMessageOrchestrator] Subscribing to {topics.Length} distinct topics: {string.Join(", ", topics)}");
                _mqttService.Subscribe(topics);
            }
        }

        private void OnMqttDataReceived(object sender, MqttData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Topic)) return;

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    List<IMqttTopicHandler> matchingHandlers;
                    lock (_handlers)
                    {
                        matchingHandlers = _handlers.Where(h => h.CanHandle(data.Topic)).ToList();
                    }

                    if (matchingHandlers.Count == 0)
                    {
                        _log?.Debug($"[MqttMessageOrchestrator] No handler found for topic: '{data.Topic}'");
                        return;
                    }

                    foreach (var handler in matchingHandlers)
                    {
                        using (var cts = new CancellationTokenSource())
                        {
                            await handler.HandleAsync(data.Topic, data, cts.Token).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log?.Error($"[MqttMessageOrchestrator] Error processing payload for topic '{data.Topic}': {ex.Message}");
                }
            });
        }

        public void Dispose()
        {
            if (_mqttService != null)
            {
                _mqttService.Connected -= OnMqttConnected;
                _mqttService.Disconnected -= OnMqttDisconnected;
                _mqttService.DataReceived -= OnMqttDataReceived;
            }
        }
    }
}
