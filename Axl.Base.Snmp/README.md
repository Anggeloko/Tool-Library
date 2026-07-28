# Tools.Snmp

Pre-packaged SNMP manipulation library.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## NuGet Dependencies
- `SnmpSharpNet` (v0.9.7)

## Technical Reference (API)

### Class `SnmpService`

#### Initialization and Usage Example

```csharp
var snmp = new SnmpService();

// GET
var oids = new List<string> { "1.3.6.1.2.1.1.1.0" };
var results = snmp.Get("Switch01", "192.168.1.10", 161, 2, oids, out string err, "public");

if (string.IsNullOrEmpty(err)) {
    foreach (var kvp in results) {
        Console.WriteLine($"OID: {kvp.Key}, Value: {kvp.Value}");
    }
}

// WALK
var walkResults = snmp.Walk("Switch01", "192.168.1.10", 161, 2, "1.3.6.1.2.1.2.2.1.2", out string walkErr, "public");
```

#### `Get(string deviceId, string ip, int port, int version, List<string> oids, out string error, ...)`
Executes an SNMP GET request to retrieve specific OID values.
- **Parameters:**
  - `deviceId`: Device ID (used for traffic locking).
  - `ip`: IP address of SNMP agent.
  - `port`: Port (typically 161).
  - `version`: `1`, `2`, or `3`.
  - `oids`: List of OIDs to query.
  - `error`: Output parameter containing error message in case of failure.
- **Return:** `Dictionary<string, string>` (Key: OID, Value: Result).

#### `Walk(string deviceId, string ip, int port, int version, string oid, out string error, ...)`
Executes an SNMP WALK request (MIB tree traversal).
- **Parameters Same as Get**, except `oid` is the root node of the walk.
- **Return:** `Dictionary<string, string>`.

#### `InterpretIfTypes(Dictionary<string, string> data, out Dictionary<int, ifTypeEl> dict)`
Maps interface type (`ifType`) query results to human-readable names.
- **Return:** `Dictionary<int, int>` (Port -> TypeID).
