# Tools.Icmp

Utilidades para comprobación de conectividad mediante ICMP (Ping).

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Technical Reference (API)

### Clase `IcmpService`

#### Usage Example

```csharp
var icmp = new IcmpService();

// Ping simple
bool alive = icmp.Ping("127.0.0.1", 500);

// Descubrimiento de IP (entre varias opciones)
var ips = new List<string> { "10.0.0.1", "10.0.0.2", "192.168.1.5" };
string bestIp = icmp.GetIp("Device01", ips, out string err);

// Ping múltiple
var results = icmp.MultiplePing(ips, 1000);
```

#### `Ping(string host, int timeout = 1000)`
Realiza un ping simple a un host.
- **Parámetros:**
  - `host`: Dirección IP o nombre de host.
  - `timeout`: Tiempo máximo de espera en milisegundos.
- **Retorno:** `bool` (True si respondió).

#### `MultiplePing(List<string> hosts, int timeout = 1000)`
Realiza pings en paralelo a una lista de hosts.
- **Retorno:** `Dictionary<string, bool>`.
