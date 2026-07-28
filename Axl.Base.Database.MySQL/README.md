# Axl.Base.Database.MySQL

Librería de acceso a datos para MySQL Server, optimizada para alto rendimiento e inserciones masivas. Implementa las interfaces `ISql` e `ICheckable`.

## Configuración e Inicialización

La librería permite la inicialización mediante un string de conexión completo, lo cual es la forma recomendada para aprovechar todas las capacidades del driver.

### Constructor

```csharp
public MySQL(string connectionString, string name = "MySQL", int timeout = 30)
```

- **connectionString**: Cadena de conexión estándar de MySQL.
- **name**: Identificador de la instancia (útil para logs).
- **timeout**: Tiempo de espera predeterminado para comandos.

> [!IMPORTANT]
> **Seguridad en Memoria (RAM):** El string de conexión se encripta inmediatamente en el constructor utilizando AES-256 y una semilla única del equipo. Solo se desencripta temporalmente al momento de abrir una conexión, minimizando la exposición de credenciales en memoria RAM.

## Integración Unificada (SqlCtr)

Compatible con el contenedor `SqlCtr` de `Axl.Base.Common`, permitiendo alternar entre proveedores sin cambiar la lógica de negocio:

```csharp
// Configuración mediante Connection String (Recomendado)
// Soporta opciones: SslMode, Pooling, Max Pool Size, Connection Timeout, etc.
var connString = "Server=127.0.0.1;Port=3306;Database=test;Uid=user;Pwd=pass;Pooling=true;Max Pool Size=50;Connection Timeout=15;SslMode=Preferred;";
var db = new MySQL(connString, "MyConn");

var context = new SqlCtr(db, (ICheckable)db);

var result = await context.Sql.GetList<User>("SELECT * FROM Users");
```

## Inserción y Sincronización Masiva (Bulk/Upsert)

#### `BulkInsert(string tableName, DataTable data)`
#### `BulkInsert<T>(string tableName, IEnumerable<T> data)`
Implementado mediante la técnica de **Inserción Multi-row** optimizada. El método procesa los datos en lotes (batches) configurables para maximizar el rendimiento y evitar límites de tamaño de paquete del servidor.

#### `Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns)`
Realiza una sincronización masiva de datos (Insert/Update) utilizando una tabla temporal staging y joins de actualización. Garantiza la atomicidad de la operación en el servidor.
- **Retorno:** `Task<Result<int>>` (total de filas afectadas).

### Monitoreo (Health Check)

#### `CheckAsync()`
Verifica la conectividad con el servidor MySQL mediante una apertura de conexión ligera.

---
*Versión: 1.1.5*
