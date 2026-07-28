# Axl.Base.Database.MySQL

Data access library for MySQL Server, optimized for high performance and bulk insertions. Implements `ISql` and `ICheckable` interfaces.

## Configuration & Initialization

The library supports initialization via a complete connection string, which is the recommended approach to leverage all driver capabilities.

### Constructor

```csharp
public MySQL(string connectionString, string name = "MySQL", int timeout = 30)
```

- **connectionString**: Standard MySQL connection string.
- **name**: Instance identifier (useful for logging).
- **timeout**: Default command timeout in seconds.

> [!IMPORTANT]
> **In-Memory Security (RAM):** The connection string is immediately encrypted in the constructor using AES-256 and a machine-unique seed. It is only temporarily decrypted when opening a connection, minimizing credential exposure in RAM.

## Unified Integration (SqlCtr)

Compatible with the `SqlCtr` container from `Axl.Base.Common`, allowing switching between database providers without altering business logic:

```csharp
// Configuration via Connection String (Recommended)
// Supports options: SslMode, Pooling, Max Pool Size, Connection Timeout, etc.
var connString = "Server=127.0.0.1;Port=3306;Database=test;Uid=user;Pwd=pass;Pooling=true;Max Pool Size=50;Connection Timeout=15;SslMode=Preferred;";
var db = new MySQL(connString, "MyConn");

var context = new SqlCtr(db, (ICheckable)db);

var result = await context.Sql.GetList<User>("SELECT * FROM Users");
```

## Bulk Insertion & Synchronization (Bulk/Upsert)

#### `BulkInsert(string tableName, DataTable data)`
#### `BulkInsert<T>(string tableName, IEnumerable<T> data)`
Implemented using an optimized **Multi-row Insertion** technique. The method processes data in configurable batches to maximize throughput and avoid server packet size limits.

#### `Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns)`
Executes bulk data synchronization (Insert/Update) using a temporary staging table and update joins. Guarantees atomic server-side operations.
- **Return:** `Task<Result<int>>` (total affected rows).

### Monitoring (Health Check)

#### `CheckAsync()`
Verifies connectivity with the MySQL server using a lightweight connection test.

---
*Version: 1.1.5*
