# Tools.MqttNet (Modern)

Advanced asynchronous MQTT client library based on `MQTTnet`.

## Prerequisites
- **Framework:** .NET Framework 4.5.2.

## NuGet Dependencies
- `MQTTnet` (v4.3.3.952)

## Technical Reference (API)

### Class `MqttNetService`

#### Usage Example

```csharp
var mqtt = new MqttNetService("broker.hivemq.com");

mqtt.DataReceived += (s, e) => {
    Console.WriteLine($"[{e.Timestamp}] {e.Topic}: {e.Payload}");
};

await mqtt.ConnectAsync();
await mqtt.SubscribeAsync(new[] { "factory/alerts", "factory/status" });

// Publish object (Automatic JSON serialization)
await mqtt.PublishAsync("factory/telemetry", new { Temp = 22.5, Status = "OK" });

// Check connection status
if (mqtt.IsConnected) {
    Console.WriteLine("Connected to broker.");
}
```

#### `bool IsConnected`
Gets a value indicating whether the client is currently connected to the MQTT broker.

#### `MqttQos Qos`
Sets the Quality of Service (QoS) level for publish and subscribe operations. Default is `AtLeastOnce` (1).
- **Values:** `AtMostOnce` (0), `AtLeastOnce` (1), `ExactlyOnce` (2).

#### `bool UseCompression`
If set to `true`, the client uses internal GZip compression for sent and received messages. Ideal for reducing bandwidth without external dependencies.

#### `Task ConnectAsync()`
Asynchronous connection to the broker. Automatically configures `ClientId` using `MachineInfo.Hash()` if none is provided in the constructor.
- **Exceptions:** Throws `Exception` with details if connection fails.

#### `Task SubscribeAsync(string[] topics)`
Subscribes the client to multiple topics in a single operation.

#### `Task PublishAsync(string topic, string payload)`
Publishes a plain text message to the specified topic.

#### `Task PublishAsync<T>(string topic, T payload)`
Automatically serializes the `payload` object to JSON and publishes it.

#### `event EventHandler Connected`
Event triggered when the asynchronous connection is established.

#### `event EventHandler Disconnected`
Event triggered when the connection is lost or closed.

#### `event EventHandler<MqttData> DataReceived`
Asynchronous event for receiving data.

> [!TIP]
> It is recommended to subscribe to `Connected` and `Disconnected` to reactively handle reconnection or UI update logic.
