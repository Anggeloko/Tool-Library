# Axl.Base.Database.MSSQL

Data access library for Microsoft SQL Server, optimized for high performance and bulk insertions using `SqlBulkCopy`. Implements `ISql` and `ICheckable` interfaces.

## Configuration & Initialization

The library supports initialization via a complete connection string, allowing configuration of advanced security and performance options.

### Constructor

```csharp
public MsSQL(string connectionString, string name = "MsSQL", int timeout = 30)
```

- **connectionString**: Standard SQL Server connection string.
- **name**: Instance identifier.
- **timeout**: Default command timeout in seconds.

> [!IMPORTANT]
> **In-Memory Security (RAM):** The connection string is immediately encrypted in the constructor using AES-256 and a machine-unique seed. It is only temporarily decrypted when opening a connection, minimizing credential exposure in RAM.

## Unified Integration (SqlCtr)

To simplify database service management, using the `SqlCtr` model is recommended:

```csharp
// Configuration via Connection String (Recommended)
// Supports advanced options: Pooling, Connect Timeout, Encrypt, TrustServerCertificate, etc.
var connString = "Server=server,1433;Initial Catalog=DB;User Id=user;Password=pass;Pooling=true;Max Pool Size=100;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;";
var provider = new msSQL(connString, "ProdDB");

var context = new SqlCtr(provider, provider);

// Usage
var health = await context.Check.CheckAsync();
if (health.IsSuccess) {
    var data = await context.Sql.GetList<MyModel>("SELECT * FROM Table");
}
```

## Bulk Insertion & Synchronization (Bulk/Upsert)

#### `BulkInsert(string tableName, DataTable data)`
Uses `SqlBulkCopy` with `BatchSize = 5000` for maximum performance.

#### `Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns)`
Executes an atomic MERGE operation on the server using a temporary staging table.

## Dictionary Helpers
Includes static helper methods for safe type extraction from result dictionaries:
- `DictString(dict, "key")`
- `DictInt(dict, "key")`
- `DictDouble(dict, "key")`
- `DictDateTime(dict, "key")`

---
*Version: 1.1.6*
