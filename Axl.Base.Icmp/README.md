# Tools.Icmp

Utilities for connectivity checks using ICMP (Ping).

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Technical Reference (API)

### Class `IcmpService`

#### Usage Example

```csharp
var icmp = new IcmpService();

// Simple ping
bool alive = icmp.Ping("127.0.0.1", 500);

// IP discovery (among multiple candidates)
var ips = new List<string> { "10.0.0.1", "10.0.0.2", "192.168.1.5" };
string bestIp = icmp.GetIp("Device01", ips, out string err);

// Multiple ping
var results = icmp.MultiplePing(ips, 1000);
```

#### `Ping(string host, int timeout = 1000)`
Executes a simple ping to a host.
- **Parameters:**
  - `host`: IP address or hostname.
  - `timeout`: Maximum wait time in milliseconds.
- **Return:** `bool` (True if it responded).

#### `MultiplePing(List<string> hosts, int timeout = 1000)`
Executes parallel pings across a list of hosts.
- **Return:** `Dictionary<string, bool>`.
