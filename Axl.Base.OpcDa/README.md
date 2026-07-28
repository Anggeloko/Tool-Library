# Tools.OpcDa

Classic OPC DA (Data Access) client library.

## Prerequisites
- **Framework:** .NET Framework 4.0.
- **Architecture:** Requires **x86** (32-bit). The process consuming this library must run in 32-bit mode due to COM/DCOM dependencies (OpcRcw). Running in x64 will result in "Attempted to read or write protected memory" errors.

## Technical Reference (API)

### Class `OpcDaService`

#### Usage Example

```csharp
var opc = new OpcDaService();
var tags = new Dictionary<string, string> {
    { "T1", "Dev1.Static.Temp" },
    { "P1", "Dev1.Static.Press" }
};

// Reading (Requires x86 process)
var res = opc.Read("PLC01", "Schneider-Aut.OFS.2", "127.0.0.1", tags, out string err);

if (string.IsNullOrEmpty(err)) {
    foreach (var item in res.Resultado)
        Console.WriteLine($"{item.Nombre}: {item.ValObj} ({item.Calidad})");
}
```

#### `Read(string deviceId, string svrName, string ip, Dictionary<string, string> parameters, out string error, int retry = 1)`
Executes a synchronous read of multiple OPC tags.
- **Parameters:**
  - `deviceId`: Device ID for exclusive traffic locking.
  - `svrName`: Name of the OPC server (e.g. `Schneider-Aut.OFS.2`).
  - `ip`: Server IP address.
  - `parameters`: Dictionary where Key is a friendly name and Value is the OPC ItemID (e.g. `{"Temp", "Dev1.Static.Var1"}`).
  - `retry`: Number of retry attempts if data quality is not "Good".
- **Return:** `OPCRes` object.
  - Contains a `Resultado` list of `OPCObj` items with `Nombre`, `ValObj` (value), `Fecha` (timestamp), and `Calidad` (quality).
- **Output:** `error` contains technical details in case of critical failure or unreadable tags.
