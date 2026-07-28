# Tools.Json.Newton

JSON serialization service implementation using the popular `Newtonsoft.Json` library.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## NuGet Dependencies
- `Newtonsoft.Json` (v13.0.3)

## Technical Reference (API)

Implements the `IJson` interface from `Tools.Common`.

### Class `JsonNewtonService`

#### Usage Example

```csharp
IJson json = new NewtonJson();

// Serialize
var data = new { User = "Axl", Role = "Admin" };
string raw = json.Serialize(data);

// Deserialize
var obj = json.Deserialize<MyModel>(raw);
```

#### `Serialize(object obj)`
Serializes any object into a JSON string using standard Newtonsoft settings.
- **Parameters:**
  - `obj`: The object to serialize.
- **Return:** `string` containing the JSON.

#### `Deserialize<T>(string json)`
Deserializes a JSON string into an instance of the specified object type.
- **Parameters:**
  - `json`: String formatted in JSON.
- **Return:** Instance of `T`.
