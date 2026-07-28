# Tools.Database.SQLite

SQLite v3 client implementation oriented towards secure (encrypted) local persistence of configurations and variables.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## NuGet Dependencies
- `System.Data.SQLite.Core` (v1.0.119)
- `Newtonsoft.Json` (v13.0.4)

## Technical Reference (API)

### Class `SQLiteService`

Manages variable storage in a local SQLite database (`settings.db`), using machine hardware-based AES-256 encryption.

#### Usage Example

```csharp
// Initialized with the folder path where settings.db will be stored
var settings = new SQLiteService("C:\\ProgramData\\MyApp");

// Save (Automatically encrypted)
settings.SetVariable("ApiKey", "12345-ABCDE");

// Retrieve (Automatically decrypted)
string key = settings.GetVariable("ApiKey", "default_if_not_found");

// Read all
var all = settings.ReadVariables();
```

#### `GetVariable(string key, string defaultValue = null)`
Retrieves the value of a specific variable.
- **Self-Healing:** If the data was saved using a previous machine identity (e.g. computer rename), the method automatically re-encrypts it using the current identity.
- **Return:** `string` (decrypted value or `defaultValue`).

#### `SetVariable(string key, string value)`
Saves or updates a specific variable. Ensures no duplicates exist by removing previous records associated with the same key under any known machine hash.

#### `DeleteVariable(string key)`
Deletes a variable from the database.

#### `ReadVariables()`
Reads all variables stored in the database.
- **Return:** `List<VariableItem>`.

#### `SaveVariables(IEnumerable<VariableItem> variables)`
Saves a complete collection of variables, overwriting the current table contents.

---

### Model `VariableItem`

Base class for managing key-value pairs with change notification support.

- **`Key`**: Variable identifier.
- **`Value`**: Variable value (automatically encrypted when persisted).
