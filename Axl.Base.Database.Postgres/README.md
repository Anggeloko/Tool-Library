# Axl.Base.Database.Postgres

Data access library for PostgreSQL, optimized for high performance via the native binary copy API (`Binary COPY`). Implements `ISql` and `ICheckable` interfaces.

## Configuration & Initialization

The library is initialized exclusively via a complete connection string.

### Constructor

```csharp
public Postgres(string connectionString, string name = "PostgresInstance", int commandTimeout = 30)
```

> [!IMPORTANT]
> **In-Memory Security (RAM):** The connection string is immediately encrypted in the constructor using AES-256 and a machine-unique seed. It is only temporarily decrypted when opening a connection, minimizing credential exposure in RAM.

## Unified Integration (SqlCtr)

```csharp
// Configuration via Connection String (Recommended)
// Supports options: SSL Mode, Pooling, Maximum Pool Size, Timeout, etc.
var connString = "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=pass;Pooling=true;Maximum Pool Size=20;Timeout=15;SSL Mode=Prefer;Trust Server Certificate=true;";
var db = new Postgres(connString, "MyPostgres");

var context = new SqlCtr(db, (ICheckable)db);

var list = await context.Sql.GetList<MyEntity>("SELECT * FROM entities");
```

## Bulk Loading & Synchronization

#### `BulkInsert(string tableName, DataTable data)`
#### `BulkInsert<T>(string tableName, IEnumerable<T> data)`
Uses `NpgsqlBinaryImporter` to perform direct binary copying to the server.

#### `Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns)`
Performs synchronization (Merge) leveraging PostgreSQL's powerful `ON CONFLICT` clause.

### Monitoring (Health Check)

#### `CheckAsync()`
Verifies connectivity with the Postgres server using a lightweight connection test.

---
*Version: 1.0.1*
