# Tools.Common

Librería base (SDK-style) que contiene los modelos, interfaces y utilidades estáticas compartidas por todo el ecosistema.

## Prerrequisitos
- **Frameworks compatibles:** .NET 4.0, .NET 4.5.

## Referencia Técnica (API)

### Modelos Principales

#### `Result<T>`
Encapsula una respuesta de operación, incluyendo el valor de retorno o una lista de errores.

```csharp
// Creación de resultados
var success = Result<int>.Success(100);
var failure = Result<int>.Failure(new List<string> { "Error de conexión" });

if (success.IsSuccess) {
    Console.WriteLine($"Valor: {success.Value}");
}
```

- **Propiedades:**
  - `Value`: El valor devuelto (tipo `T`). `default(T)` si falló.
  - `Errors`: `List<string>` con los mensajes de error.
  - `IsSuccess`: `bool` que indica si la operación fue exitosa (`Errors.Count == 0`).

### Utilidades Estáticas

#### `MachineInfo`
Provee información detallada del hardware y sistema operativo.

```csharp
// Identificador único persistente (16 caracteres hexadecimales)
string hardwareId = MachineInfo.Hash(16);

// Métricas de sistema
var cpu = MachineInfo.Cpu();
Console.WriteLine($"CPU: {cpu.Name} Usage: {cpu.Usage}%");
```

- **`Hash(int len = 16)`**: Genera un identificador único persistente para la máquina basado en hardware (CPU, MAC, Board, Disk). Devuelve un `string` hexadecimal.
- **`Cpu()`**: Devuelve objeto `CpuData` con uso de CPU, núcleos y nombre del procesador.
- **`Ram()`**: Devuelve `RamData` con memoria total, disponible y porcentaje de uso.
- **`Drives()`**: Devuelve `List<DriveData>` con espacio y labels de todos los discos listos.
- **`Services()`**: Devuelve `List<ServiceData>` con el estado de todos los servicios del sistema.

#### `DataConverterUtils`
Utilidades para conversión de estructuras de datos.

```csharp
var list = new List<MyModel> { ... };
DataTable dt = list.ToDataTable(); // Extensión automática
```

- **`ToDataTable<T>(this IEnumerable<T> items)`**: Convierte una lista de objetos en un `DataTable` de forma dinámica.

#### `TrafficControl` (Portero)
Gestiona el acceso exclusivo a recursos de hardware para evitar colisiones de hilos/procesos.

```csharp
if (TrafficControl.StartExclusiveHeavy("COM1")) {
    try {
        // Operación exclusiva en puerto serie
    } finally {
        TrafficControl.StopExclusiveHeavy("COM1");
    }
}
```

- **`StartExclusiveHeavy(string deviceId)`**: Intenta obtener un bloqueo para el ID de dispositivo. Devuelve `bool`.
- **`StopExclusiveHeavy(string deviceId)`**: Libera el bloqueo del dispositivo.

#### `Encryption`
Funciones rápidas de cifrado simétrico (Base64 + Ofuscación básica).

```csharp
string encrypted = Encryption.Encrypt("mi_secreto");
string decrypted = Encryption.Decrypt(encrypted);
```

- **`Encrypt(string plainText)`**: Devuelve string cifrado.
- **`Decrypt(string cipherText)`**: Devuelve string original.

### Interfaces Core
- **`ISql`**: Base para `MSSQL`, `MySQL`, `Postgres` y `SQLite`.
  - `BulkInsert<T>(table, data)`: Inserción masiva de alto rendimiento desde objetos.
  - `Upsert<T>(table, data, keys)`: Sincronización masiva (Merge) de objetos contra una tabla.
- **`IOpc`**: Base para `OpcUa`.

---
*Versión: 1.1.9*
