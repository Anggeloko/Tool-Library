# Tools.Logs.SQL

Extensión de logging para persistencia en base de datos.

## Prerrequisitos
- **Framework:** .NET Framework 4.5.2.

## Referencia Técnica (API)

### Clase `SQLog`

Implementa `ILog` y requiere una instancia de `ISql` para funcionar.

#### Ejemplo de Uso

```csharp
ISql db = new MSSQL("LogDB", "server", 1433, "Logs", "user", "pass");
IJson json = new NewtonJson();

ILog sqlLog = new SQLog(json, db, "app_logs");

sqlLog.Write("Evento registrado en SQL");
```

#### Constructor: `SQLog(ISql sqlService, string table = "logs")`
- **Parámetros:**
  - `sqlService`: Cualquier implementación de `ISql` (`MSSQL`, `MySQL`, `SQLite`).
  - `table`: Nombre de la tabla donde se insertarán los registros.

#### Funcionalidad
Cada vez que se llama a `Write` o sus variantes, se ejecuta una inserción asíncrona en la base de datos con los campos:
- `Timestamp`: Fecha y hora.
- `Level`: `INFO`, `ERROR`, `WARN`, `DEBUG`.
- `Caller`: Nombre del método emisor.
- `Message`: Contenido del log.
- `Exception`: Detalles de la excepción si existen.
