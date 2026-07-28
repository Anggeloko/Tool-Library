# Tools.Mqtt (Legacy)

Legacy synchronous MQTT client library.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Technical Reference (API)

### Class `MQTTService`

#### Usage Example

```csharp
var mqtt = new MQTTService("localhost");

mqtt.DataReceived += (s, e) => {
    Console.WriteLine($"Topic: {e.Topic}, Message: {e.Payload}");
};

mqtt.Connect();

if (mqtt.IsConnected) {
    mqtt.Subscribe(new[] { "sensors/temp" });
    mqtt.Publish("commands/reset", "1");
}
```

#### `bool IsConnected`
Gets a value indicating whether the client is currently connected to the MQTT broker.

#### `MqttQos Qos`
Sets the Quality of Service (QoS) level for publish and subscribe operations. Default is `AtLeastOnce` (1).
- **Values:** `AtMostOnce` (0), `AtLeastOnce` (1), `ExactlyOnce` (2).

#### `bool UseCompression`
If set to `true`, the client uses internal GZip compression for sent and received messages. Ideal for reducing bandwidth without external dependencies.

#### `Connect()`
Establishes connection to the broker using parameters provided in the constructor.
- **Exceptions:** Throws `Exception` if the connection fails.

#### `Subscribe(string[] topics)`
Subscribes the client to multiple topics.

#### `Publish(string topic, string message)`
Sends a message to the broker.

#### `event EventHandler Connected`
Event triggered upon successfully establishing connection with the broker.

#### `event EventHandler Disconnected`
Event triggered when connection to the broker is lost or manually closed.

#### `event EventHandler<MqttData> DataReceived`
Event triggered when a message arrives on a subscribed topic.
- **MqttData:** Contains `Topic`, `Payload`, and `Timestamp`.

> [!TIP]
> Always verify the `IsConnected` property before performing publish or subscribe operations to prevent unhandled exceptions.
