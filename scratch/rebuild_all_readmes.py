import os

# Complete dictionary mapping all Spanish terms/sentences to English
WORD_MAP = [
    ("Librería de manipulación WMI pre-empaquetada para extracción de métricas de Windows.", "Pre-packaged WMI manipulation library for Windows metrics extraction."),
    ("Librería de extracción de métricas de hardware a través de la API Redfish de HPE iLO.", "Hardware metrics extraction library via HPE iLO Redfish API."),
    ("Librería para la consulta de contadores de rendimiento y métricas del sistema operativo Windows a través de WMI/CIM.", "Library for querying Windows OS performance counters and system metrics via WMI/CIM."),
    ("Librería de monitoreo y consulta de dispositivos de red mediante el protocolo SNMP v1/v2c.", "Network device monitoring and querying library using SNMP v1/v2c protocol."),
    ("Librería ligera para verificación de conectividad y latencia mediante ICMP Echo (Ping).", "Lightweight library for verifying network connectivity and latency using ICMP Echo (Ping)."),
    ("Conector y consulta de datos históricos a AVEVA / Wonderware Historian.", "Connector and historical data query for AVEVA / Wonderware Historian."),
    ("Cliente industrial OPC UA para lectura, escritura y suscripciones en tiempo real.", "Industrial OPC UA client for real-time reading, writing, and subscriptions."),
    ("Cliente industrial legacy OPC Data Access (OPC DA Classic).", "Legacy industrial OPC Data Access client (OPC DA Classic)."),
    ("Cliente de mensajería MQTT para integración con brokers IoT.", "MQTT messaging client for integration with IoT brokers."),
    ("Abstracción genérica de repositorios y patrón Unit of Work.", "Generic repository abstraction and Unit of Work pattern."),
    ("Adaptador de acceso a datos para Microsoft SQL Server.", "Data access adapter for Microsoft SQL Server."),
    ("Adaptador de acceso a datos para MySQL / MariaDB.", "Data access adapter for MySQL / MariaDB."),
    ("Adaptador de acceso a datos para PostgreSQL.", "Data access adapter for PostgreSQL."),
    ("Adaptador ligero de acceso a datos para bases de datos SQLite.", "Lightweight data access adapter for SQLite databases."),
    ("Almacenamiento y gestión segura de secretos y credenciales en caché con encriptación.", "Secure storage and management of cached secrets and credentials with encryption."),
    ("Acceso y manipulación de archivos en recursos compartidos de red (SMB/UNC) con impersonación de credenciales.", "File access and manipulation on network shares (SMB/UNC) with credential impersonation."),
    ("Transferencia segura de archivos vía protocolo SFTP.", "Secure file transfer via SFTP protocol."),
    ("Generación y lectura de hojas de cálculo Excel.", "Excel spreadsheet generation and reading library."),
    ("Compresión y descompresión nativa de archivos.", "Native file compression and decompression library."),
    ("Lectura y escritura persistente en el Registro de Windows.", "Persistent reading and writing to the Windows Registry."),
    ("Ejecución de tareas programadas y trabajadores en segundo plano.", "Scheduled task execution and background workers."),
    ("Cliente HTTP resiliente y cliente REST para integraciones web.", "Resilient HTTP client and REST client for web integrations."),
    ("Motor de logging unificado con soporte para almacenamiento en BD SQL.", "Unified logging engine with SQL database storage support."),
    ("Utilidades compartidas, tipos comunes y clases base.", "Shared utilities, common types, and base classes."),
    ("Serialización y deserialización de JSON.", "JSON serialization and deserialization."),
    ("Cliente de integración con OMDb API.", "OMDb API integration client."),
    ("Librería para la integración con la base de datos OM (Operational Management) de Foxboro mediante comandos de consola.", "Library for integration with Foxboro OM (Operational Management) database via console commands."),
    ("Proyecto centralizado de pruebas unitarias y de integración para todo el ecosistema.", "Centralized unit and integration testing project for the entire ecosystem."),
    ("Proyecto de pruebas unitarias para validar las funcionalidades de las librerías base.", "Unit testing project to validate base library functionalities."),
    
    # Section Headings
    ("## Prerrequisitos", "## Prerequisites"),
    ("## Referencia Técnica (API)", "## Technical Reference (API)"),
    ("### Puerto/Interfaz", "### Port/Interface"),
    ("### Clase", "### Class"),
    ("### Interfaz", "### Interface"),
    ("#### Métodos", "#### Methods"),
    ("#### Propiedades", "#### Properties"),
    ("#### Eventos", "#### Events"),
    ("#### Constructor", "#### Constructor"),
    ("### Adaptador de Infraestructura", "### Infrastructure Adapter"),
    ("#### Ejemplo de Uso", "#### Usage Example"),
    ("## Ejemplo de Uso", "## Usage Example"),
    ("### Modelo", "### Model"),
    ("### Modelos", "### Models"),
    ("## Dependencias de NuGet", "## NuGet Dependencies"),
    ("## Dependencias Internas", "## Internal Dependencies"),
    ("## Cobertura de Pruebas", "## Test Coverage"),
    ("## Ejemplo de Estructura de Test", "## Test Structure Example"),

    # Field labels & parameters
    ("* **Parámetros:**", "* **Parameters:**"),
    ("- **Parámetros:**", "- **Parameters:**"),
    ("* **Retorno:**", "* **Return:**"),
    ("- **Retorno:**", "- **Return:**"),
    ("- **Valores:**", "- **Values:**"),
    ("- **Excepciones:**", "- **Exceptions:**"),
    ("- **Salida:**", "- **Output:**"),
    ("- **Propiedades:**", "- **Properties:**"),
    ("- **Tipos soportados (`OmType`):**", "- **Supported Types (`OmType`):**"),

    # Comments in code blocks
    ("// Usa d:\\opt\\fox\\bin\\tools por defecto", "// Uses d:\\opt\\fox\\bin\\tools by default"),
    ("// Escribir un booleano", "// Write a boolean"),
    ("// Leer un float", "// Read a float"),
    ("Temperatura:", "Temperature:"),
    ("Versión:", "Version:"),
    ("Salud del Servidor:", "Server Health:"),
    ("Consumo Energético:", "Power Consumption:"),
    ("Conectado al broker.", "Connected to broker.")
]

def clean_translate(text):
    for old, new in WORD_MAP:
        text = text.replace(old, new)
    
    text = text.replace("Librería de ", "Library for ")
    text = text.replace("Librería para ", "Library for ")
    text = text.replace("Cliente de ", "Client for ")
    text = text.replace("Cliente para ", "Client for ")
    text = text.replace("Motor de ", "Engine for ")
    text = text.replace("Servicio de ", "Service for ")
    text = text.replace("Soporte para ", "Support for ")
    text = text.replace("para la integración con", "for integration with")
    text = text.replace("mediante comandos de consola", "via console commands")
    text = text.replace("Serialización automática a JSON", "Automatic JSON serialization")

    return text

docs_dir = r"c:\Users\Axl\Desktop\Projects\Libraries\Docs"
base_dir = r"c:\Users\Axl\Desktop\Projects\Libraries"

for fname in os.listdir(docs_dir):
    if not fname.endswith(".md"):
        continue

    doc_path = os.path.join(docs_dir, fname)
    with open(doc_path, "r", encoding="utf-8") as f:
        spanish_content = f.read()

    translated_content = clean_translate(spanish_content)

    proj_name = fname[:-3]
    proj_dir = os.path.join(base_dir, proj_name)

    if os.path.exists(proj_dir):
        target_readme = os.path.join(proj_dir, "README.md")
        with open(target_readme, "w", encoding="utf-8") as f:
            f.write(translated_content)

print("Rebuilding complete.")
