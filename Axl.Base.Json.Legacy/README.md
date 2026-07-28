# Tools.Json.Legacy

JSON serialization library optimized for backward compatibility.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Technical Reference (API)

### Class `JsonService`

#### Usage Example

```csharp
var legacyJson = new LegacyJson();

// Standard IJson operations
string json = legacyJson.Serialize(new { Id = 1 });
var item = legacyJson.Deserialize<MyItem>(json);
```

#### `Serialize(object obj)`
Converts an object into its JSON string representation.
- **Return:** `string`.

#### `Deserialize<T>(string json)`
Converts a JSON string into an object of the specified type.
- **Return:** `T`.
