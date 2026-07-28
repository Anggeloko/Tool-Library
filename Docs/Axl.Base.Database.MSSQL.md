# Axl.Base.Database.MSSQL

Librería de acceso a datos para Microsoft SQL Server, optimizada para alto rendimiento e inserciones masivas mediante `SqlBulkCopy`. Implementa las interfaces `ISql` e `ICheckable`.

## Configuración e Inicialización

La librería permite la inicialización mediante un string de conexión completo, permitiendo configurar opciones avanzadas de seguridad y rendimiento.

### Constructor

```csharp
public MsSQL(string connectionString, string name = "MsSQL", int timeout = 30)
```

- **connectionString**: Cadena de conexión estándar de SQL Server.
- **name**: Identificador de la instancia.
- **timeout**: Tiempo de espera predeterminado para comandos.

> [!IMPORTANT]
> **Seguridad en Memoria (RAM):** El string de conexión se encripta inmediatamente en el constructor utilizando AES-256 y una semilla única del equipo. Solo se desencripta temporalmente al momento de abrir una conexión, minimizando la exposición de credenciales en memoria RAM.

## Integración Unificada (SqlCtr)

Para facilitar el manejo de servicios de base de datos, se recomienda usar el modelo `SqlCtr`:

```csharp
// Configuración mediante Connection String (Recomendado)
// Soporta opciones avanzadas: Pooling, Connect Timeout, Encrypt, TrustServerCertificate, etc.
var connString = "Server=server,1433;Initial Catalog=DB;User Id=user;Password=pass;Pooling=true;Max Pool Size=100;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;";
var provider = new msSQL(connString, "ProdDB");

var context = new SqlCtr(provider, provider);

// Uso
var health = await context.Check.CheckAsync();
if (health.IsSuccess) {
    var data = await context.Sql.GetList<MyModel>("SELECT * FROM Table");
}
```

## Inserción y Sincronización Masiva (Bulk/Upsert)

#### `BulkInsert(string tableName, DataTable data)`
Utiliza `SqlBulkCopy` con `BatchSize = 5000` para máximo rendimiento.

#### `Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns)`
Realiza una operación MERGE atómica en el servidor utilizando una tabla temporal staging.

## Helpers de Diccionario
Incluye métodos estáticos para extracción segura de tipos desde diccionarios de resultados:
- `DictString(dict, "key")`
- `DictInt(dict, "key")`
- `DictDouble(dict, "key")`
- `DictDateTime(dict, "key")`

---
*Versión: 1.1.6*
