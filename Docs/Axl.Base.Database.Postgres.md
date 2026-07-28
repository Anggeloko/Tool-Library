# Axl.Base.Database.Postgres

Librería de acceso a datos para PostgreSQL, optimizada para alto rendimiento mediante la API nativa de copia binaria (`Binary COPY`). Implementa las interfaces `ISql` e `ICheckable`.

## Configuración e Inicialización

La librería se inicializa exclusivamente mediante un string de conexión completo.

### Constructor

```csharp
public Postgres(string connectionString, string name = "PostgresInstance", int commandTimeout = 30)
```

> [!IMPORTANT]
> **Seguridad en Memoria (RAM):** El string de conexión se encripta inmediatamente en el constructor utilizando AES-256 y una semilla única del equipo. Solo se desencripta temporalmente al momento de abrir una conexión, minimizando la exposición de credenciales en memoria RAM.

## Integración Unificada (SqlCtr)

```csharp
// Configuración mediante Connection String (Recomendado)
// Soporta opciones: SSL Mode, Pooling, Maximum Pool Size, Timeout, etc.
var connString = "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=pass;Pooling=true;Maximum Pool Size=20;Timeout=15;SSL Mode=Prefer;Trust Server Certificate=true;";
var db = new Postgres(connString, "MyPostgres");

var context = new SqlCtr(db, (ICheckable)db);

var list = await context.Sql.GetList<MyEntity>("SELECT * FROM entities");
```

## Carga y Sincronización Masiva

#### `BulkInsert(string tableName, DataTable data)`
#### `BulkInsert<T>(string tableName, IEnumerable<T> data)`
Utiliza `NpgsqlBinaryImporter` para realizar una copia binaria directa al servidor.

#### `Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns)`
Realiza una sincronización (Merge) utilizando la potencia de la cláusula `ON CONFLICT` de PostgreSQL.

### Monitoreo (Health Check)

#### `CheckAsync()`
Verifica la conectividad con el servidor Postgres mediante una apertura de conexión ligera.

---
*Versión: 1.0.1*
