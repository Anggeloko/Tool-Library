# Tools.OpcUa

Librería para cliente OPC UA moderna y asíncrona.

## Prerrequisitos
- **Framework:** .NET Framework 4.7.2.

## Referencia Técnica (API)

### Clase `OpcUaClient`

#### Ejemplo de Uso

```csharp
string endpoint = "opc.tcp://localhost:4840";
using (var client = new OpcUaClient(endpoint))
{
    await client.ConnectAsync();

    // Lectura de nodos
    var nodes = new List<string> { "ns=2;s=Device1.Temperature", "ns=2;s=Device1.Pressure" };
    var result = await client.ReadNodesAsync(nodes);

    if (result.IsSuccess) {
        foreach (var kvp in result.Value)
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }

    // Navegación (Browse)
    var children = await client.BrowseNodesAsync("ns=2;s=Device1");
}
```

#### `Task ConnectAsync()`
Establece la sesión con el servidor. Maneja automáticamente el autodescubrimiento de endpoints y la confianza de certificados.
- **Excepciones:** Lanza `Exception` si la conexión o el descubrimiento fallan.

#### `Task<Result<Dictionary<string, string>>> ReadNodesAsync(List<string> nodeIds)`
Lee el valor actual de una lista de NodeIds.
- **Parámetros:**
  - `nodeIds`: Lista de cadenas con los IDs de los nodos (ej. `ns=2;s=Device1.Temperature`).
- **Retorno:** `Result<Dictionary<string, string>>`. El diccionario usa el NodeId como clave y el valor convertido a string como contenido.

#### `Task<Result<Dictionary<string, string>>> BrowseNodesAsync(string rootNodeId)`
Explora los nodos hijos de un nodo raíz y lee sus valores actuales.
- **Retorno:** `Result<Dictionary<string, string>>` con todos los nodos encontrados en el primer nivel de profundidad.
