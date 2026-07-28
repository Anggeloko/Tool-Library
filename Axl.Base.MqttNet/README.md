# Tools.MqttNet (Moderno)

Librería avanzada para cliente MQTT asíncrono basada en `MQTTnet`.

## Prerequisites
- **Framework:** .NET Framework 4.5.2.

## Dependencias de NuGet
- `MQTTnet` (v4.3.3.952)

## Technical Reference (API)

### Clase `MqttNetService`

#### Usage Example

```csharp
var mqtt = new MqttNetService("broker.hivemq.com");

mqtt.DataReceived += (s, e) => {
    Console.WriteLine($"[{e.Timestamp}] {e.Topic}: {e.Payload}");
};

await mqtt.ConnectAsync();
await mqtt.SubscribeAsync(new[] { "factory/alerts", "factory/status" });

// Publicar objeto (Serialización automática a JSON)
await mqtt.PublishAsync("factory/telemetry", new { Temp = 22.5, Status = "OK" });

// Verificar estado de conexión
if (mqtt.IsConnected) {
    Console.WriteLine("Conectado al broker.");
}
```

#### `bool IsConnected`
Obtiene un valor que indica si el cliente está actualmente conectado al broker MQTT.

#### `MqttQos Qos`
Define el nivel de calidad de servicio (QoS) para las operaciones de publicación y suscripción. Por defecto es `AtLeastOnce` (1).
- **Valores:** `AtMostOnce` (0), `AtLeastOnce` (1), `ExactlyOnce` (2).

#### `bool UseCompression`
Si se establece en `true`, el cliente utilizará compresión GZip interna para los mensajes enviados y recibidos. Esto es ideal para reducir el ancho de banda sin dependencias externas.

#### `Task ConnectAsync()`
Conexión asíncrona al broker. Configura automáticamente el `ClientId` usando `MachineInfo.Hash()` si no se provee uno en el constructor.
- **Excepciones:** Lanza `Exception` con detalles si la conexión falla.

#### `Task SubscribeAsync(string[] topics)`
Suscribe el cliente a múltiples tópicos en una sola operación.

#### `Task PublishAsync(string topic, string payload)`
Publica un mensaje de texto plano en el tópico indicado.

#### `Task PublishAsync<T>(string topic, T payload)`
Serializa automáticamente el objeto `payload` a JSON y lo publica.

#### `event EventHandler Connected`
Evento que se dispara cuando la conexión asíncrona se establece.

#### `event EventHandler Disconnected`
Evento que se dispara cuando la conexión se pierde o se cierra.

#### `event EventHandler<MqttData> DataReceived`
Evento asíncrono para recepción de datos.

> [!TIP]
> Se recomienda suscribirse a `Connected` y `Disconnected` para gestionar la lógica de reconexión o actualización de UI de forma reactiva.
