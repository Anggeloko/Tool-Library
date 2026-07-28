import os

TRANSLATIONS_FULL = [
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
    ("Dependencias:", "Dependencies:"),
    ("para serialización JSON nativa", "for native JSON serialization"),
    ("Realiza consultas a la API Redfish de iLO para recopilar información de energía, temperaturas, procesadores, almacenamiento, interfaces de red y eventos de hardware.", "Queries HPE iLO Redfish API to collect power, temperature, processor, storage, network interface, and hardware event information."),
    ("Dirección IP o host del servidor iLO.", "IP address or host of the iLO server."),
    ("Usuario con permisos de lectura.", "User with read permissions."),
    ("Contraseña del usuario.", "User password."),
    ("Objeto `IloMetrics` con los datos parseados y detalles del estado de salud general.", "`IloMetrics` object with parsed data and overall health details."),
    ("Implementación concreta de `IIloService` para interactuar con la API Redfish de HPE iLO v4/v5+.", "Concrete implementation of `IIloService` to interact with HPE iLO v4/v5+ Redfish API."),
    ("// Configurar seguridad TLS y certificados (necesario para iLO)", "// Configure TLS security and certificates (required for iLO)"),
    ("Salud del Servidor:", "Server Health:"),
    ("Consumo Energético:", "Power Consumption:"),
    ("Clase que encapsula los datos recolectados:", "Class encapsulating collected data:"),
    ("Fecha/hora (UTC) de la recolección.", "Collection Timestamp (UTC)."),
    ("IP o Host consultado.", "Queried IP or Host."),
    ("Consumo total del chasis en Watts.", "Total chassis power consumption in Watts."),
    ("Consumo individual de las fuentes de poder.", "Individual power supply consumption in Watts."),
    ("Voltaje de entrada registrado.", "Recorded input voltage."),
    ("Temperaturas de CPU, entrada y máximo de discos en °C.", "CPU, inlet, and maximum disk temperatures in °C."),
    ("Velocidad promedio de los ventiladores en porcentaje.", "Average fan speed percentage."),
    ("Estado general de salud del sistema (ej. OK, Warning, Critical).", "Overall system health status (e.g., OK, Warning, Critical)."),
    ("Estado de salud del subsistema de almacenamiento.", "Storage subsystem health status."),
    ("Listado descriptivo de los procesadores instalados y su estado.", "Descriptive list of installed processors and their status."),
    ("Listado descriptivo de las interfaces de red y su estado.", "Descriptive list of network interfaces and their status."),
    ("Últimos 5 eventos registrados en el log IML de hardware.", "Last 5 events recorded in the hardware IML log."),
    ("Diccionario con errores o campos adicionales extraídos durante la consulta.", "Dictionary containing errors or additional fields extracted during the query.")
]

docs_dir = r"c:\Users\Axl\Desktop\Projects\Libraries\Docs"

for fname in os.listdir(docs_dir):
    if fname.endswith(".md"):
        path = os.path.join(docs_dir, fname)
        with open(path, "r", encoding="utf-8") as f:
            content = f.read()

        # Structural replacements
        content = content.replace("## Prerrequisitos", "## Prerequisites")
        content = content.replace("## Referencia Técnica (API)", "## Technical Reference (API)")
        content = content.replace("### Puerto/Interfaz", "### Port/Interface")
        content = content.replace("#### Métodos", "#### Methods")
        content = content.replace("* **Parámetros:**", "* **Parameters:**")
        content = content.replace("* **Retorno:**", "* **Return:**")
        content = content.replace("### Adaptador de Infraestructura", "### Infrastructure Adapter")
        content = content.replace("#### Ejemplo de Uso", "#### Usage Example")
        content = content.replace("### Modelo", "### Model")

        for old, new in TRANSLATIONS_FULL:
            content = content.replace(old, new)

        proj_name = fname[:-3]
        target_dir = os.path.join(r"c:\Users\Axl\Desktop\Projects\Libraries", proj_name)
        if os.path.exists(target_dir):
            readme_path = os.path.join(target_dir, "README.md")
            with open(readme_path, "w", encoding="utf-8") as f:
                f.write(content)
            print(f"Full translation written to {readme_path}")
