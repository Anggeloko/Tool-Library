# Tools.Logs

Librería base para servicios de registro (Logging).

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Technical Reference (API)

Implementa la interfaz `ILog`. Dependiendo de la implementación utilizada (`FileLog`, `ScreenLog`), el destino de la traza variará.

### Ejemplo de Uso

```csharp
IJson json = new NewtonJson();

// Log a archivo (Rota diariamente)
ILog log = new FileLog(json, "Production", "C:\\Logs");

// Log a consola
ILog screen = new ScreenLog(json);

log.Write("Iniciando servicio...");
log.WriteError("Fallo crítico en motor", new Exception("Stack overflow"));
```

### Métodos Principales

#### `Write(string message, [CallerMemberName] string caller = "")`
Escribe un mensaje de información.
- **Parámetros:**
  - `message`: El texto a registrar.
  - `caller`: Se llena automáticamente con el nombre del método que llamó a la función.

#### `WriteError(string message, Exception ex = null, ...)`
Escribe una traza de error, opcionalmente incluyendo el stack trace de una excepción.

#### `WriteWarning(string message, ...)`
Escribe una traza de advertencia.

#### `WriteDebug(string message, ...)`
Escribe trazas detalladas de depuración (solo visibles si la configuración del logger lo permite).
