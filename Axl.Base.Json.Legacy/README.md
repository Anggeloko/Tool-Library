# Tools.Json.Legacy

Librería para serialización JSON optimizada para compatibilidad.

## Prerrequisitos
- **Framework:** .NET Framework 4.0.

## Referencia Técnica (API)

### Clase `JsonService`

#### Ejemplo de Uso

```csharp
var legacyJson = new LegacyJson();

// Operaciones estándar IJson
string json = legacyJson.Serialize(new { Id = 1 });
var item = legacyJson.Deserialize<MyItem>(json);
```

#### `Serialize(object obj)`
Convierte un objeto a su representación en cadena JSON.
- **Retorno:** `string`.

#### `Deserialize<T>(string json)`
Convierte una cadena JSON a un objeto del tipo especificado.
- **Retorno:** `T`.
