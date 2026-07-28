import os
import re

# Comprehensive list of Spanish phrases found in Docs/*.md files
TRANSLATION_RULES = [
    ("Librería avanzada para cliente MQTT asíncrono basada en", "Advanced asynchronous MQTT client library based on"),
    ("Librería de cliente MQTT basada en", "MQTT client library based on"),
    ("Librería de manipulación WMI pre-empaquetada para extracción de métricas de Windows.", "Pre-packaged WMI manipulation library for Windows metrics extraction."),
    ("Proyecto de pruebas unitarias para validar las funcionalidades de las librerías base.", "Unit testing project to validate base library functionalities."),
    ("Librería de extracción de métricas de hardware a través de la API Redfish de HPE iLO.", "Hardware metrics extraction library via HPE iLO Redfish API."),
    ("Librería para la consulta de contadores de rendimiento y métricas del sistema operativo Windows a través de WMI/CIM.", "Library for querying Windows OS performance counters and system metrics via WMI/CIM."),
    ("Librería de monitoreo y consulta de dispositivos de red mediante el protocolo SNMP v1/v2c.", "Network device monitoring and querying library using SNMP v1/v2c protocol."),
    ("Librería ligera para verificación de conectividad y latencia mediante ICMP Echo (Ping).", "Lightweight library for verifying network connectivity and latency using ICMP Echo (Ping)."),
    
    # Headings
    ("## Prerrequisitos", "## Prerequisites"),
    ("## Referencia Técnica (API)", "## Technical Reference (API)"),
    ("### Puerto/Interfaz", "### Port/Interface"),
    ("### Clase", "### Class"),
    ("### Interfaz", "### Interface"),
    ("#### Métodos", "#### Methods"),
    ("#### Propiedades", "#### Properties"),
    ("#### Eventos", "#### Events"),
    ("* **Parámetros:**", "* **Parameters:**"),
    ("- **Parámetros:**", "- **Parameters:**"),
    ("* **Retorno:**", "* **Return:**"),
    ("- **Retorno:**", "- **Return:**"),
    ("### Adaptador de Infraestructura", "### Infrastructure Adapter"),
    ("#### Ejemplo de Uso", "#### Usage Example"),
    ("### Modelo", "### Model"),
    ("### Modelos", "### Models"),
    ("## Dependencias de NuGet", "## NuGet Dependencies"),
    ("## Dependencias Internas", "## Internal Dependencies"),
    ("## Cobertura de Pruebas", "## Test Coverage"),

    # Code comments & Descriptions
    ("Publicar objeto (Serialización automática a JSON)", "Publish object (Automatic JSON serialization)"),
    ("Assert estado de conexión", "Assert connection status"),
    ("Conectado al broker.", "Connected to broker."),
    ("Gets un valor que indica si el cliente está actualmente conectado al broker MQTT.", "Gets a value indicating whether the client is currently connected to the MQTT broker."),
    ("Evento que se dispara cuando se recibe un mensaje en un tópico suscrito.", "Event triggered when a message is received on a subscribed topic."),
    ("Se dispara al establecer conexión exitosamente con el broker.", "Triggered upon successfully establishing a connection with the broker."),
    ("Se dispara cuando se pierde la conexión con el broker.", "Triggered when connection to the broker is lost."),
    ("Conecta asíncronamente al broker configurado.", "Asynchronously connects to the configured broker."),
    ("Desconecta asíncronamente del broker.", "Asynchronously disconnects from the broker."),
    ("Se suscribe a un listado de tópicos MQTT.", "Subscribes to a list of MQTT topics."),
    ("Publica un payload como string en un tópico específico.", "Publishes a string payload to a specific topic."),
    ("Publica un objeto serializado automáticamente a JSON.", "Publishes an object automatically serialized to JSON."),
    ("Estructura de evento para mensajes recibidos:", "Event payload structure for received messages:"),
    ("Tópico del mensaje.", "Message topic."),
    ("Contenido del mensaje como string.", "Message payload as string."),
    ("Marca de tiempo UTC de recepción.", "UTC reception timestamp."),
    ("Nivel de Calidad de Servicio (QoS).", "Quality of Service (QoS) level.")
]

def translate_text(text):
    for old, new in TRANSLATION_RULES:
        text = text.replace(old, new)
    
    # Regex fallback replacements for common patterns
    text = re.sub(r"\bGets un valor que indica\b", "Gets a value indicating", text)
    text = re.sub(r"\bRealiza consultas\b", "Executes queries", text)
    text = re.sub(r"\bSe dispara\b", "Triggered", text)
    text = re.sub(r"\bConecta asíncronamente\b", "Asynchronously connects", text)
    text = re.sub(r"\bDesconecta asíncronamente\b", "Asynchronously disconnects", text)
    text = re.sub(r"\bSe suscribe a\b", "Subscribes to", text)
    text = re.sub(r"\bPublica un\b", "Publishes a", text)
    text = re.sub(r"\bEstructura de\b", "Structure for", text)
    text = re.sub(r"\bObjeto\b", "Object", text)
    text = re.sub(r"\bLista de\b", "List of", text)
    text = re.sub(r"\bCredenciales\b", "Credentials", text)
    text = re.sub(r"\bMensaje en caso de fallo\b", "Error message on failure", text)

    return text

docs_dir = r"c:\Users\Axl\Desktop\Projects\Libraries\Docs"
base_dir = r"c:\Users\Axl\Desktop\Projects\Libraries"

for fname in os.listdir(docs_dir):
    if not fname.endswith(".md"):
        continue

    doc_path = os.path.join(docs_dir, fname)
    with open(doc_path, "r", encoding="utf-8") as f:
        raw_text = f.read()

    translated = translate_text(raw_text)

    proj_name = fname[:-3]
    target_readme = os.path.join(base_dir, proj_name, "README.md")

    if os.path.exists(os.path.dirname(target_readme)):
        with open(target_readme, "w", encoding="utf-8") as f:
            f.write(translated)
        print(f"Updated {target_readme}")
