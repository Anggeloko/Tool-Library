# Tools.Common

Base library (SDK-style) containing shared models, interfaces, and static utilities used across the entire ecosystem.

## Prerequisites
- **Compatible Frameworks:** .NET 4.0, .NET 4.5.

## Technical Reference (API)

### Main Models

#### `Result<T>`
Encapsulates an operation response, including the return value or a list of errors.

```csharp
// Result creation
var success = Result<int>.Success(100);
var failure = Result<int>.Failure(new List<string> { "Connection error" });

if (success.IsSuccess) {
    Console.WriteLine($"Value: {success.Value}");
}
```

- **Properties:**
  - `Value`: The returned value (type `T`). `default(T)` if it failed.
  - `Errors`: `List<string>` containing error messages.
  - `IsSuccess`: `bool` indicating whether the operation succeeded (`Errors.Count == 0`).

### Static Utilities

#### `MachineInfo`
Provides detailed hardware and operating system information.

```csharp
// Persistent unique identifier (16 hexadecimal characters)
string hardwareId = MachineInfo.Hash(16);

// System metrics
var cpu = MachineInfo.Cpu();
Console.WriteLine($"CPU: {cpu.Name} Usage: {cpu.Usage}%");
```

- **`Hash(int len = 16)`**: Generates a persistent hardware-based unique identifier for the machine (CPU, MAC, Board, Disk). Returns a hexadecimal `string`.
- **`Cpu()`**: Returns a `CpuData` object with CPU usage, cores, and processor name.
- **`Ram()`**: Returns `RamData` with total memory, available memory, and usage percentage.
- **`Drives()`**: Returns `List<DriveData>` with space and labels for all ready drives.
- **`Services()`**: Returns `List<ServiceData>` with the status of all system services.

#### `DataConverterUtils`
Utilities for data structure conversions.

```csharp
var list = new List<MyModel> { ... };
DataTable dt = list.ToDataTable(); // Automatic extension method
```

- **`ToDataTable<T>(this IEnumerable<T> items)`**: Dynamically converts a list of objects into a `DataTable`.

#### `TrafficControl` (Gatekeeper)
Manages exclusive access to hardware resources to prevent thread/process collisions.

```csharp
if (TrafficControl.StartExclusiveHeavy("COM1")) {
    try {
        // Exclusive operation on serial port
    } finally {
        TrafficControl.StopExclusiveHeavy("COM1");
    }
}
```

- **`StartExclusiveHeavy(string deviceId)`**: Attempts to acquire a lock for the specified device ID. Returns `bool`.
- **`StopExclusiveHeavy(string deviceId)`**: Releases the lock for the specified device.

#### `Encryption`
Fast symmetric encryption helper functions (Base64 + basic obfuscation).

```csharp
string encrypted = Encryption.Encrypt("my_secret");
string decrypted = Encryption.Decrypt(encrypted);
```

- **`Encrypt(string plainText)`**: Returns encrypted string.
- **`Decrypt(string cipherText)`**: Returns decrypted original string.

### Core Interfaces
- **`ISql`**: Base interface for `MSSQL`, `MySQL`, `Postgres`, and `SQLite`.
  - `BulkInsert<T>(table, data)`: High-performance bulk insertion from objects.
  - `Upsert<T>(table, data, keys)`: Bulk synchronization (Merge) of objects against a table.
- **`IOpc`**: Base interface for `OpcUa`.

---
*Version: 1.1.9*
