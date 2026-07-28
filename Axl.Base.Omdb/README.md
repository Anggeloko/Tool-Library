# Tools.Omdb

Library for integration with Foxboro OM (Operational Management) database via console commands.

## Prerequisites
- **Foxboro Tools:** Foxboro toolset must be installed (e.g. `omgetimp`, `omsetimp`, `omcrt`, `omfnd`).
- **Shell:** Requires `ksh.exe` available on the system.
- **Default path:** `d:\opt\fox\bin\tools`.

## Technical Reference (API)

### Class `OmdbService`

#### Constructor `OmdbService(string workingDirectory = DefaultFoxboroPath)`
Initializes the service verifying that the toolset path exists.
- **Parameters:**
  - `workingDirectory`: Optional path to tools. If not provided, uses standard Foxboro path on drive D.

#### `Write(string variable, object value, OmType type)`
Writes a value to the database. If the variable does not exist, it creates it automatically with the specified type.
- **Supported Types (`OmType`):** `Bool`, `String`, `Float`, `Int`, `Long`.

#### `Read<T>(string variable, out bool exists)`
Reads the value of a variable.
- **Return:** The value converted to generic type `T`.
- **Output:** `exists` indicates whether the variable was found.

#### `ReadBit(string variable, int bit, out bool exists)`
Reads a specific bit of a Packed Long (`pl`) variable.
- **Parameters:**
  - `variable`: PL variable name.
  - `bit`: Bit index (0-31).

#### `CreateRawVariables(string prefix, int quantity, OmType type)`
Creates a sequence of variables formatted as `PREFIX_001`, `PREFIX_002`, etc.

## Usage Example

```csharp
var service = new OmdbService(); // Uses d:\opt\fox\bin\tools by default

// Write a boolean
service.Write("MI_VAR_BOOL", true, OmType.Bool);

// Read a float
float temp = service.Read<float>("TEMP_001", out bool exists);

if (exists) {
    Console.WriteLine($"Temperature: {temp}");
}

---
*Version: 1.0.2*
```
