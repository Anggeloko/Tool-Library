# Tools.Logs

Base logging services library.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Technical Reference (API)

Implements the `ILog` interface. Depending on the concrete implementation used (`FileLog`, `ScreenLog`), the log output target will vary.

### Usage Example

```csharp
IJson json = new NewtonJson();

// File logger (Rotates daily)
ILog log = new FileLog(json, "Production", "C:\\Logs");

// Console logger
ILog screen = new ScreenLog(json);

log.Write("Starting service...");
log.WriteError("Critical engine failure", new Exception("Stack overflow"));
```

### Main Methods

#### `Write(string message, [CallerMemberName] string caller = "")`
Writes an informational message.
- **Parameters:**
  - `message`: The text to log.
  - `caller`: Automatically populated with the name of the calling method.

#### `WriteError(string message, Exception ex = null, ...)`
Writes an error log entry, optionally including an exception's stack trace.

#### `WriteWarning(string message, ...)`
Writes a warning log entry.

#### `WriteDebug(string message, ...)`
Writes detailed debug logs (only visible if allowed by the logger configuration).
