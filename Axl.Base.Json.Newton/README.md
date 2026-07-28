# Tools.Json.Newton

Implementación del servicio de serialización JSON utilizando la popular librería `Newtonsoft.Json`.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Dependencias de NuGet
- `Newtonsoft.Json` (v13.0.3)

## Technical Reference (API)

Implementa la interfaz `IJson` de `Tools.Common`.

### Clase `JsonNewtonService`

#### Usage Example

```csharp
IJson json = new NewtonJson();

// Serializar
var data = new { User = "Axl", Role = "Admin" };
string raw = json.Serialize(data);

// Deserializar
var obj = json.Deserialize<MyModel>(raw);
```

#### `Serialize(object obj)`
Serializa cualquier objeto a una cadena JSON utilizando los ajustes estándar de Newtonsoft.
- **Parámetros:**
  - `obj`: El objeto a serializar.
- **Retorno:** `string` conteniendo el JSON.

#### `Deserialize<T>(string json)`
Deserializa una cadena JSON al tipo de objeto especificado.
- **Parámetros:**
  - `json`: Cadena de texto en formato JSON.
- **Retorno:** Instancia de `T`.
