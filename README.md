# Axl.Base Libraries Solution

Modular collection of .NET libraries built following **Hexagonal Architecture** principles, focused on infrastructure monitoring, industrial connectivity, databases, security, file management, and background utilities.

---

## 📚 Project and Library Structure

Below is a summary of the components included in the solution along with links to their respective documentation:

### 🖥️ Infrastructure & Monitoring
* [**Axl.Base.Ilo**](Axl.Base.Ilo/README.md): Hardware metrics extraction (temperature, power supplies, storage, events) via HPE iLO Redfish API.
* [**Axl.Base.Wmi**](Axl.Base.Wmi/README.md): Windows OS performance counters and system metrics query via WMI/CIM.
* [**Axl.Base.Snmp**](Axl.Base.Snmp/README.md): Network device monitoring and querying via SNMP v1/v2c protocol.
* [**Axl.Base.Icmp**](Axl.Base.Icmp/README.md): Connectivity and latency verification using ICMP Echo (Ping).

### 🏭 Industrial Connectivity (OT) & IoT
* [**Axl.Base.Historian.WW**](Axl.Base.Historian.WW/README.md): Connector and historical data query for AVEVA / Wonderware Historian.
* [**Axl.Base.OpcUa**](Axl.Base.OpcUa/README.md): Industrial OPC UA client for real-time reading, writing, and subscriptions.
* [**Axl.Base.OpcDa**](Axl.Base.OpcDa/README.md): Industrial legacy OPC Data Access client (OPC DA Classic).
* [**Axl.Base.Mqtt**](Axl.Base.Mqtt/README.md) / [**Axl.Base.MqttNet**](Axl.Base.MqttNet/README.md): MQTT messaging client for integration with IoT brokers.

### 💾 Persistence & Databases
* [**Axl.Base.Persistence**](Axl.Base.Persistence/README.md): Generic repository abstraction and Unit of Work pattern.
* [**Axl.Base.Database.MSSQL**](Axl.Base.Database.MSSQL/README.md): Data access adapter for Microsoft SQL Server.
* [**Axl.Base.Database.MySQL**](Axl.Base.Database.MySQL/README.md): Data access adapter for MySQL / MariaDB.
* [**Axl.Base.Database.Postgres**](Axl.Base.Database.Postgres/README.md): Data access adapter for PostgreSQL.
* [**Axl.Base.Database.SQLite**](Axl.Base.Database.SQLite/README.md): Lightweight data access adapter for SQLite databases.

### 🔐 Security & Caching
* [**Axl.Base.Security.Cache**](Axl.Base.Security.Cache/README.md): Secure storage and management of cached secrets and credentials with encryption.

### 📁 Files, Network & Formats
* [**Axl.Base.NetworkFileAccess**](Axl.Base.NetworkFileAccess/README.md): File access and manipulation on network shares (SMB/UNC) with credential impersonation.
* [**Axl.Base.Sftp**](Axl.Base.Sftp/README.md): Secure file transfer via SFTP protocol.
* [**Axl.Base.Excel**](Axl.Base.Excel/README.md): Generation and reading of Excel spreadsheets.
* [**Axl.Base.Compression.Native**](Axl.Base.Compression.Native/README.md): Native file compression and decompression.
* [**Axl.Base.RegistryStore**](Axl.Base.RegistryStore/README.md): Persistent reading and writing to the Windows Registry.

### ⚙️ Services & Internal Logistics
* [**Axl.Base.Background**](Axl.Base.Background/README.md): Scheduled task execution and background workers.
* [**Axl.Base.Http**](Axl.Base.Http/README.md): Resilient HTTP client and REST client for web integrations.
* [**Axl.Base.Logs**](Axl.Base.Logs/README.md) / [**Axl.Base.Logs.SQL**](Axl.Base.Logs.SQL/README.md): Unified logging engine with support for SQL DB storage.
* [**Axl.Base.Common**](Axl.Base.Common/README.md): Shared utilities, common types, and base classes.
* [**Axl.Base.Json.Newton**](Axl.Base.Json.Newton/README.md) / [**Axl.Base.Json.Legacy**](Axl.Base.Json.Legacy/README.md): JSON serialization and deserialization.
* [**Axl.Base.Omdb**](Axl.Base.Omdb/README.md): Integration client for OMDb API.

---

## 🛠️ Development Requirements
* **Recommended IDE:** Visual Studio 2022 / VS Code
* **Target Framework:** .NET Framework / .NET Core / .NET Standard (depending on the package)

---

## 📝 License & Contribution
Private project for internal use in integrated monitoring and infrastructure solutions.
