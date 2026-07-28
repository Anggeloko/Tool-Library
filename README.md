# Axl.Base Libraries Solution

Colección modular de librerías .NET diseñadas bajo principios de **Arquitectura Hexagonal**, enfocadas en monitoreo de infraestructura, conectividad industrial, bases de datos, seguridad, gestión de archivos y utilidades de fondo.

---

## 📚 Estructura de Proyectos y Librerías

A continuación se resume la biblioteca de componentes incluidas en la solución y sus enlaces a la documentación respectiva:

### 🖥️ Monitoreo e Infraestructura
* [**Axl.Base.Ilo**](Axl.Base.Ilo/README.md): Extracción de métricas de hardware (temperatura, fuentes, almacenamiento, eventos) vía API Redfish de HPE iLO.
* [**Axl.Base.Wmi**](Axl.Base.Wmi/README.md): Consulta de contadores de rendimiento y métricas del sistema operativo Windows vía WMI/CIM.
* [**Axl.Base.Snmp**](Axl.Base.Snmp/README.md): Monitoreo y consulta de dispositivos de red mediante protocolo SNMP v1/v2c.
* [**Axl.Base.Icmp**](Axl.Base.Icmp/README.md): Verificación de conectividad y latencia mediante ICMP Echo (Ping).

### 🏭 Conectividad OT / Industrial & IoT
* [**Axl.Base.Historian.WW**](Axl.Base.Historian.WW/README.md): Conector y consulta de datos históricos a AVEVA / Wonderware Historian.
* [**Axl.Base.OpcUa**](Axl.Base.OpcUa/README.md): Cliente industrial OPC UA para lectura, escritura y suscripciones en tiempo real.
* [**Axl.Base.OpcDa**](Axl.Base.OpcDa/README.md): Cliente industrial legacy OPC Data Access (OPC DA Classic).
* [**Axl.Base.Mqtt**](Axl.Base.Mqtt/README.md) / [**Axl.Base.MqttNet**](Axl.Base.MqttNet/README.md): Cliente de mensajería MQTT para integración con brokers IoT.

### 💾 Persistencia y Bases de Datos
* [**Axl.Base.Persistence**](Axl.Base.Persistence/README.md): Abstracción genérica de repositorios y patrón Unit of Work.
* [**Axl.Base.Database.MSSQL**](Axl.Base.Database.MSSQL/README.md): Adaptador de acceso a datos para Microsoft SQL Server.
* [**Axl.Base.Database.MySQL**](Axl.Base.Database.MySQL/README.md): Adaptador de acceso a datos para MySQL / MariaDB.
* [**Axl.Base.Database.Postgres**](Axl.Base.Database.Postgres/README.md): Adaptador de acceso a datos para PostgreSQL.
* [**Axl.Base.Database.SQLite**](Axl.Base.Database.SQLite/README.md): Adaptador ligero de acceso a datos para bases de datos SQLite.

### 🔐 Seguridad y Caché
* [**Axl.Base.Security.Cache**](Axl.Base.Security.Cache/README.md): Almacenamiento y gestión segura de secretos y credenciales en caché con encriptación.

### 📁 Archivos, Red y Formatos
* [**Axl.Base.NetworkFileAccess**](Axl.Base.NetworkFileAccess/README.md): Acceso y manipulación de archivos en recursos compartidos de red (SMB/UNC) con impersonación de credenciales.
* [**Axl.Base.Sftp**](Axl.Base.Sftp/README.md): Transferencia segura de archivos vía protocolo SFTP.
* [**Axl.Base.Excel**](Axl.Base.Excel/README.md): Generación y lectura de hojas de cálculo Excel.
* [**Axl.Base.Compression.Native**](Axl.Base.Compression.Native/README.md): Compresión y descompresión nativa de archivos.
* [**Axl.Base.RegistryStore**](Axl.Base.RegistryStore/README.md): Lectura y escritura persistente en el Registro de Windows.

### ⚙️ Servicios y Logística Interna
* [**Axl.Base.Background**](Axl.Base.Background/README.md): Ejecución de tareas programadas y trabajadores en segundo plano.
* [**Axl.Base.Http**](Axl.Base.Http/README.md): Cliente HTTP resiliente y cliente REST para integraciones web.
* [**Axl.Base.Logs**](Axl.Base.Logs/README.md) / [**Axl.Base.Logs.SQL**](Axl.Base.Logs.SQL/README.md): Motor de logging unificado con soporte para almacenamiento en BD SQL.
* [**Axl.Base.Common**](Axl.Base.Common/README.md): Utilidades compartidas, tipos comunes y clases base.
* [**Axl.Base.Json.Newton**](Axl.Base.Json.Newton/README.md) / [**Axl.Base.Json.Legacy**](Axl.Base.Json.Legacy/README.md): Serialización y deserialización de JSON.
* [**Axl.Base.Omdb**](Axl.Base.Omdb/README.md): Cliente de integración con OMDb API.

---

## 🛠️ Requisitos de Desarrollo
* **IDE recomendado:** Visual Studio 2022 / VS Code
* **Target Framework:** .NET Framework / .NET Core / .NET Standard (según el paquete)

---

## 📝 Licencia y Contribución
Proyecto privado para uso en soluciones integradas de monitoreo e infraestructura.
