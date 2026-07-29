# Axl.Base.Orchestrator.Mqtt 📡

Library for dynamic MQTT topic routing, autonomous subscription management, and auto-reconnection resilience compatible with legacy and modern MQTT implementations.

## Features
- **Dual Compatibility**: Operates seamlessly with `Axl.Base.Mqtt` (M2Mqtt) and `Axl.Base.MqttNet` (MQTTnet) via `IMqttService`.
- **Autonomous Subscriptions**: Collects required topics directly from registered `IMqttTopicHandler` strategies upon connection/reconnection.
- **Asynchronous Non-blocking Dispatch**: Dispatches incoming MQTT payloads to background tasks (`Task.Factory.StartNew`), ensuring the network listener loop remains unblocked.

## Creating a New Handler

Implement `IMqttTopicHandler` to declare a topic strategy:

```csharp
public class TelemetryTopicHandler : IMqttTopicHandler
{
    public string SubscriptionTopic => "TELEMETRY/+/DATA";

    public bool CanHandle(string topic) => topic.StartsWith("TELEMETRY/", StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(string topic, MqttData data, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received {data.Payload} from {topic}");
        await Task.Yield();
    }
}
```

## Declaration & Usage Example

```csharp
// 1. Instantiate transport client (legacy or MqttNet)
IMqttService mqttService = new MqttNetService("127.0.0.1", 1883);

// 2. Register handlers
var handlers = new IMqttTopicHandler[] { new TelemetryTopicHandler() };

// 3. Create Orchestrator & Connect
var orchestrator = new MqttMessageOrchestrator(mqttService, handlers, logger);
mqttService.Connect();
```
