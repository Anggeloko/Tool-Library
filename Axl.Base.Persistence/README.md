# Axl.Base.Persistence

Library for robust local variable and configuration persistence. Provides mechanisms for saving serialized objects into `.bin` files featuring hardware-linked encryption and GZip compression.

## Main Features

- **Security**: AES-256 encryption utilizing a machine hardware-derived hash (`MachineInfo`).
- **Efficiency**: Automatic compression via `GZipStream`.
- **Maintenance**: Optional automatic cleanup of old files.
- **Abstraction**: `IVariableStorage` interface to decouple business logic from physical storage.

## Installation

Add the reference to your project or install via NuGet:

```xml
<ProjectReference Include="..\Axl.Base.Persistence\Axl.Base.Persistence.csproj" />
```

## Basic Usage

### Initialization

Injecting the persistence service with a base directory path, a JSON provider (such as `LegacyJson`), and a Logger is recommended.

```csharp
using Axl.Base.Persistence.Interfaces;
using Axl.Base.Persistence.Services;
using Axl.Base.Json.Legacy;
using Axl.Base.Logs;

// ...
string basePath = @"C:\ProgramData\MyApp\Config";
IVariableStorage storage = new LocalVariableStorage(basePath, new LegacyJson(), new FileLog());
```

### Saving a Variable

```csharp
var myConfig = new { Server = "127.0.0.1", Port = 8080 };
storage.Save("CONNECTION_SETTINGS", myConfig);
```

### Loading a Variable

```csharp
var config = storage.Load<MyConfigClass>("CONNECTION_SETTINGS");
if (config != null) 
{
    // Use configuration...
}
```

### Deleting a Variable

```csharp
storage.Delete("CONNECTION_SETTINGS");
```

#### `bool UseCompression`
Defines whether files are stored compressed using GZip. Default is `true`.
- **Note:** If set to false, new files will be encrypted plain text, but the library will still attempt to decompress existing files if GZip format is detected.

## Cleanup Configuration (FileCleaner)

The constructor accepts an optional `cleanupDays` parameter (default `1`):

- **If > 0**: Automatically deletes files in the data directory older than the specified number of days.
- **If <= 0**: Disables automatic cleanup.

```csharp
// Disable automatic cleanup
var storage = new LocalVariableStorage(path, json, log, cleanupDays: 0);
```

## Dependencies

- `Axl.Base.Common`: For encryption, hardware hashes, and cleanup utilities.
- `Axl.Base.Interfaces`: For `IJson` and `ILog`.
