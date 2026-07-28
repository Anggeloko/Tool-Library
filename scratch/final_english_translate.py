import os
import re

# Complete map covering every Spanish phrase across all Docs/*.md files
EXACT_TRANSLATIONS = {
    # Headings
    "## Prerrequisitos": "## Prerequisites",
    "## Dependencias de NuGet": "## NuGet Dependencies",
    "## Dependencias Internas": "## Internal Dependencies",
    "## Referencia Técnica (API)": "## Technical Reference (API)",
    "### Puerto/Interfaz": "### Port/Interface",
    "### Clase": "### Class",
    "### Interfaz": "### Interface",
    "### Modelo": "### Model",
    "### Modelos": "### Models",
    "### Adaptador de Infraestructura": "### Infrastructure Adapter",
    "#### Métodos": "#### Methods",
    "#### Propiedades": "#### Properties",
    "#### Eventos": "#### Events",
    "#### Ejemplo de Uso": "#### Usage Example",
    "## Cobertura de Pruebas": "## Test Coverage",
    "## Ejemplo de Estructura de Test": "## Test Structure Example",

    # Labels
    "* **Parámetros:**": "* **Parameters:**",
    "- **Parámetros:**": "- **Parameters:**",
    "* **Retorno:**": "* **Return:**",
    "- **Retorno:**": "- **Return:**",
    "- **Valores:**": "- **Values:**",
    "- **Excepciones:**": "- **Exceptions:**",

    # Descriptions & Paragraphs
    "Librería avanzada para cliente MQTT asíncrono basada en": "Advanced asynchronous MQTT client library based on",
    "Librería de cliente MQTT basada en M2Mqtt para .NET Framework 4.0.": "MQTT client library based on M2Mqtt for .NET Framework 4.0.",
    "Librería de manipulación WMI pre-empaquetada para extracción de métricas de Windows.": "Pre-packaged WMI manipulation library for Windows metrics extraction.",
    "Proyecto de pruebas unitarias para validar las funcionalidades de las librerías base.": "Unit testing project to validate base library functionality.",
    "Librería de extracción de métricas de hardware a través de la API Redfish de HPE iLO.": "Hardware metrics extraction library via HPE iLO Redfish API.",
    "Librería para la consulta de contadores de rendimiento y métricas del sistema operativo Windows a través de WMI/CIM.": "Library for querying Windows OS performance counters and system metrics via WMI/CIM.",
    "Librería de monitoreo y consulta de dispositivos de red mediante el protocolo SNMP v1/v2c.": "Network device monitoring and querying library using SNMP v1/v2c protocol.",
    "Librería ligera para verificación de conectividad y latencia mediante ICMP Echo (Ping).": "Lightweight library for verifying network connectivity and latency using ICMP Echo (Ping).",
    "Obtiene un valor que indica si el cliente está actualmente conectado al broker MQTT.": "Gets a value indicating whether the client is currently connected to the MQTT broker.",
    "Define el nivel de calidad de servicio (QoS) para las operaciones de publicación y suscripción. Por defecto es `AtLeastOnce` (1).": "Sets the Quality of Service (QoS) level for publish and subscribe operations. Default is `AtLeastOnce` (1).",
    "Si se establece en `true`, el cliente utilizará compresión GZip interna para los mensajes enviados y recibidos. Esto es ideal para reducir el ancho de banda sin dependencias externas.": "If set to `true`, the client uses internal GZip compression for sent and received messages. Ideal for reducing bandwidth without external dependencies.",
    "Conexión asíncrona al broker. Configura automáticamente el `ClientId` usando `MachineInfo.Hash()` si no se provee uno en el constructor.": "Asynchronous connection to the broker. Automatically configures `ClientId` using `MachineInfo.Hash()` if none is provided in the constructor.",
    "Lanza `Exception` con detalles si la conexión falla.": "Throws `Exception` with details if the connection fails.",
    "Suscribe el cliente a múltiples tópicos en una sola operación.": "Subscribes the client to multiple topics in a single operation.",
    "Publica un mensaje de texto plano en el tópico indicado.": "Publishes a plain text message to the specified topic.",
    "Serializa automáticamente el objeto `payload` a JSON y lo publica.": "Automatically serializes the `payload` object to JSON and publishes it.",
    "Evento que se dispara cuando la conexión asíncrona se establece.": "Event triggered when the asynchronous connection is established.",
    "Evento que se dispara cuando la conexión se pierde o se cierra.": "Event triggered when the connection is lost or closed.",
    "Evento asíncrono para recepción de datos.": "Asynchronous event for receiving data.",
    "Se recomienda suscribirse a `Connected` y `Disconnected` para gestionar la lógica de reconexión o actualización de UI de forma reactiva.": "It is recommended to subscribe to `Connected` and `Disconnected` to reactively handle reconnection or UI update logic.",
    "Publicar objeto (Serialización automática a JSON)": "Publish object (Automatic JSON serialization)",
    "Conectado al broker.": "Connected to broker.",
    "Verificar estado de conexión": "Verify connection status"
}

REGEX_RULES = [
    (r"Librería de ", "Library for "),
    (r"Librería para ", "Library for "),
    (r"Cliente de ", "Client for "),
    (r"Cliente para ", "Client for "),
    (r"Motor de ", "Engine for "),
    (r"Servicio de ", "Service for "),
    (r"Soporte para ", "Support for "),
    (r"Lanza `Exception` con detalles si la conexión falla\.", "Throws `Exception` with details if connection fails."),
    (r"Publica un mensaje de texto plano en el tópico indicado\.", "Publishes a plain text message to the specified topic.")
]

docs_dir = r"c:\Users\Axl\Desktop\Projects\Libraries\Docs"
base_dir = r"c:\Users\Axl\Desktop\Projects\Libraries"

for fname in os.listdir(docs_dir):
    if not fname.endswith(".md"):
        continue

    doc_path = os.path.join(docs_dir, fname)
    with open(doc_path, "r", encoding="utf-8") as f:
        text = f.read()

    for k, v in EXACT_TRANSLATIONS.items():
        text = text.replace(k, v)

    for pattern, repl in REGEX_RULES:
        text = re.sub(pattern, repl, text)

    proj_name = fname[:-3]
    target_readme = os.path.join(base_dir, proj_name, "README.md")

    if os.path.exists(os.path.dirname(target_readme)):
        with open(target_readme, "w", encoding="utf-8") as f:
            f.write(text)

print("Translation script finished completely.")
