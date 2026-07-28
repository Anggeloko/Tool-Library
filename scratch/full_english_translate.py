import os

# Complete dictionary mapping all remaining Spanish sentences in Docs/*.md files to English
TRANSLATIONS = [
    # MqttNet
    ("Obtiene un valor que indica si el cliente está actualmente conectado al broker MQTT.", "Gets a value indicating whether the client is currently connected to the MQTT broker."),
    ("Define el nivel de calidad de servicio (QoS) para las operaciones de publicación y suscripción. Por defecto es", "Sets the Quality of Service (QoS) level for publish and subscribe operations. Default is"),
    ("Si se establece en `true`, el cliente utilizará compresión GZip interna para los mensajes enviados y recibidos. Esto es ideal para reducir el ancho de banda sin dependencias externas.", "If set to `true`, the client uses internal GZip compression for sent and received messages. Ideal for reducing bandwidth without external dependencies."),
    ("Conexión asíncrona al broker. Configura automáticamente el `ClientId` usando `MachineInfo.Hash()` si no se provee uno en el constructor.", "Asynchronous connection to the broker. Automatically configures `ClientId` using `MachineInfo.Hash()` if none is provided in the constructor."),
    ("Excepciones: Lanza `Exception` con detalles si la conexión falla.", "Exceptions: Throws `Exception` with details if the connection fails."),
    ("Suscribe el cliente a múltiples tópicos en una sola operación.", "Subscribes the client to multiple topics in a single operation."),
    ("Publishes a mensaje de texto plano en el tópico indicado.", "Publishes a plain text message to the specified topic."),
    ("Serializa automáticamente el objeto `payload` a JSON y lo publica.", "Automatically serializes the `payload` object to JSON and publishes it."),
    ("Evento que se dispara cuando la conexión asíncrona se establece.", "Event triggered when the asynchronous connection is established."),
    ("Evento que se dispara cuando la conexión se pierde o se cierra.", "Event triggered when the connection is lost or closed."),
    ("Evento asíncrono para recepción de datos.", "Asynchronous event for receiving data."),
    ("Se recomienda suscribirse a `Connected` y `Disconnected` para gestionar la lógica de reconexión o actualización de UI de forma reactiva.", "It is recommended to subscribe to `Connected` and `Disconnected` to reactively handle reconnection or UI update logic."),
    ("// Verificar estado de conexión", "// Verify connection status"),
    ("Verificar estado de conexión", "Verify connection status"),
    
    # Common words/phrases
    ("Librería de", "Library for"),
    ("Librería para", "Library for"),
    ("Cliente de", "Client for"),
    ("Cliente para", "Client for"),
    ("Motor de", "Engine for"),
    ("Servicio de", "Service for"),
    ("Soporte para", "Support for"),
    ("Obtiene", "Gets"),
    ("Guarda", "Saves"),
    ("Elimina", "Deletes"),
    ("Actualiza", "Updates"),
    ("Ejecuta", "Executes"),
    ("Consulta", "Queries"),
    ("Valores:", "Values:"),
    ("Excepciones:", "Exceptions:"),
    ("Parámetros:", "Parameters:"),
    ("Retorno:", "Return:")
]

docs_dir = r"c:\Users\Axl\Desktop\Projects\Libraries\Docs"
base_dir = r"c:\Users\Axl\Desktop\Projects\Libraries"

for fname in os.listdir(docs_dir):
    if not fname.endswith(".md"):
        continue

    doc_path = os.path.join(docs_dir, fname)
    with open(doc_path, "r", encoding="utf-8") as f:
        text = f.read()

    # Generic header translations
    text = text.replace("## Prerrequisitos", "## Prerequisites")
    text = text.replace("## Referencia Técnica (API)", "## Technical Reference (API)")
    text = text.replace("### Puerto/Interfaz", "### Port/Interface")
    text = text.replace("### Clase", "### Class")
    text = text.replace("### Interfaz", "### Interface")
    text = text.replace("#### Métodos", "#### Methods")
    text = text.replace("#### Propiedades", "#### Properties")
    text = text.replace("#### Eventos", "#### Events")
    text = text.replace("* **Parámetros:**", "* **Parameters:**")
    text = text.replace("- **Parámetros:**", "- **Parameters:**")
    text = text.replace("* **Retorno:**", "* **Return:**")
    text = text.replace("- **Retorno:**", "- **Return:**")
    text = text.replace("### Adaptador de Infraestructura", "### Infrastructure Adapter")
    text = text.replace("#### Ejemplo de Uso", "#### Usage Example")
    text = text.replace("### Modelo", "### Model")

    for old, new in TRANSLATIONS:
        text = text.replace(old, new)

    proj_name = fname[:-3]
    target_readme = os.path.join(base_dir, proj_name, "README.md")
    
    if os.path.exists(os.path.dirname(target_readme)):
        with open(target_readme, "w", encoding="utf-8") as f:
            f.write(text)

print("Done updating all README files with full English translation.")
