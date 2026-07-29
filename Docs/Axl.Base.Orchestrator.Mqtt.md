# Documentación Técnica: Axl.Base.Orchestrator.Mqtt 📡

## 1. Descripción General
`Axl.Base.Orchestrator.Mqtt` es la librería encargada de la orquestación, suscripción autónoma y despacho asíncrono de mensajes en redes MQTT.

Está construida sobre la interfaz de transporte `IMqttService` (`Axl.Base.Common`), ofreciendo **compatibilidad dual transparente** con los clientes `Axl.Base.Mqtt` (legacy M2Mqtt) y `Axl.Base.MqttNet` (MQTTnet moderno).

---

## 2. Puertos Principales (Interfaces)

### `IMqttTopicHandler`
Puerto de entrada que define la estrategia para procesar mensajes en tópicos específicos.
- **`SubscriptionTopic`**: Patrón de tópico para suscripción (admite wildcards `+` y `#`).
- **`CanHandle(string topic)`**: Comprueba si el mensaje entrante debe ser procesado por la estrategia.
- **`HandleAsync(string topic, MqttData data, CancellationToken cancellationToken)`**: Procesa el payload de forma aislada.

---

## 3. Ejemplos de Uso y Extensibilidad

### A. Creación y Declaración de un Nuevo Handler de Tópico (`IMqttTopicHandler`)
Para procesar un nuevo tipo de mensaje MQTT (ej. estado de salud del nodo), implementa `IMqttTopicHandler`:

```csharp
public class HealthTopicHandler : IMqttTopicHandler
{
    public string SubscriptionTopic => "OTMS/+/HEALTH";

    public bool CanHandle(string topic) => topic.EndsWith("/HEALTH", StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(string topic, MqttData data, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[HEALTH] Recibido payload en '{topic}': {data.Payload}");
        await Task.Yield();
    }
}
```

### B. Instanciación e Integración (Cliente Legacy vs Moderno)

#### Opción 1: Con `Axl.Base.Mqtt` (Legacy M2Mqtt)
```csharp
var mqttService = new MqttService("127.0.0.1", 1883, "CLIENT_ID_LEGACY");
var handlers = new IMqttTopicHandler[] { new HealthTopicHandler() };

var orchestrator = new MqttMessageOrchestrator(mqttService, handlers, log);
mqttService.Connect(); // Al conectar, suscribe automáticamente "OTMS/+/HEALTH"
```

#### Opción 2: Con `Axl.Base.MqttNet` (Moderno MQTTnet)
```csharp
var mqttNetService = new MqttNetService("127.0.0.1", 1883, "CLIENT_ID_NET");
var handlers = new IMqttTopicHandler[] { new HealthTopicHandler() };

var orchestrator = new MqttMessageOrchestrator(mqttNetService, handlers, log);
await mqttNetService.ConnectAsync(); // Al conectar o reconectarse, suscribe dinámicamente
```
