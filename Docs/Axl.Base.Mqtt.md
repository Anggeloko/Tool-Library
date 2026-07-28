# Tools.Mqtt (Legacy)

Librería para cliente MQTT legacy síncrono.

## Prerrequisitos
- **Framework:** .NET Framework 4.0.

## Referencia Técnica (API)

### Clase `MQTTService`

#### Ejemplo de Uso

```csharp
var mqtt = new MQTTService("localhost");

mqtt.DataReceived += (s, e) => {
    Console.WriteLine($"Tópico: {e.Topic}, Mensaje: {e.Payload}");
};

mqtt.Connect();

if (mqtt.IsConnected) {
    mqtt.Subscribe(new[] { "sensores/temp" });
    mqtt.Publish("comandos/reset", "1");
}
```

#### `bool IsConnected`
Obtiene un valor que indica si el cliente está actualmente conectado al broker MQTT.

#### `MqttQos Qos`
Define el nivel de calidad de servicio (QoS) para las operaciones de publicación y suscripción. Por defecto es `AtLeastOnce` (1).
- **Valores:** `AtMostOnce` (0), `AtLeastOnce` (1), `ExactlyOnce` (2).

#### `bool UseCompression`
Si se establece en `true`, el cliente utilizará compresión GZip interna para los mensajes enviados y recibidos. Esto es ideal para reducir el ancho de banda sin dependencias externas.

#### `Connect()`
Establece la conexión con el broker utilizando los parámetros provistos en el constructor.
- **Excepciones:** Lanza `Exception` si la conexión falla.

#### `Subscribe(string[] topics)`
Suscribe el cliente a múltiples tópicos.

#### `Publish(string topic, string message)`
Envía un mensaje al broker.

#### `event EventHandler Connected`
Evento que se dispara cuando la conexión con el broker se establece exitosamente.

#### `event EventHandler Disconnected`
Evento que se dispara cuando se pierde la conexión con el broker o se cierra manualmente.

#### `event EventHandler<MqttData> DataReceived`
Evento que se dispara cuando llega un mensaje de un tópico suscrito.
- **MqttData:** Contiene `Topic`, `Payload` y `Timestamp`.

> [!TIP]
> Siempre verifique la propiedad `IsConnected` antes de realizar operaciones de publicación o suscripción para evitar errores catastróficos.
