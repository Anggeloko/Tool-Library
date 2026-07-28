# Tools.OpcUa

Modern asynchronous OPC UA client library.

## Prerequisites
- **Framework:** .NET Framework 4.7.2.

## Technical Reference (API)

### Class `OpcUaClient`

#### Usage Example

```csharp
string endpoint = "opc.tcp://localhost:4840";
using (var client = new OpcUaClient(endpoint))
{
    await client.ConnectAsync();

    // Reading nodes
    var nodes = new List<string> { "ns=2;s=Device1.Temperature", "ns=2;s=Device1.Pressure" };
    var result = await client.ReadNodesAsync(nodes);

    if (result.IsSuccess) {
        foreach (var kvp in result.Value)
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }

    // Browsing nodes
    var children = await client.BrowseNodesAsync("ns=2;s=Device1");
}
```

#### `Task ConnectAsync()`
Establishes a session with the server. Automatically handles endpoint auto-discovery and certificate trusting.
- **Exceptions:** Throws `Exception` if connection or endpoint discovery fails.

#### `Task<Result<Dictionary<string, string>>> ReadNodesAsync(List<string> nodeIds)`
Reads the current value of a list of NodeIds.
- **Parameters:**
  - `nodeIds`: List of strings containing NodeIds (e.g. `ns=2;s=Device1.Temperature`).
- **Return:** `Result<Dictionary<string, string>>`. The dictionary uses the NodeId as key and the string-converted value as content.

#### `Task<Result<Dictionary<string, string>>> BrowseNodesAsync(string rootNodeId)`
Browses child nodes under a root node and reads their current values.
- **Return:** `Result<Dictionary<string, string>>` with all child nodes discovered on the first depth level.
