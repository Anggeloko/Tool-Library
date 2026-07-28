# Axl.Base.Ilo

Hardware metrics extraction library via HPE iLO Redfish API.

## Prerequisites
- **Framework:** .NET Framework 4.0.
- **Dependencies:** `System.Web.Extensions` (for native JSON serialization).

## Technical Reference (API)

### Port/Interface `IIloService`

#### Methods

##### `IloMetrics GetMetrics(string ip, string username, string password)`
Queries HPE iLO Redfish API to collect power, temperature, processor, storage, network interface, and hardware event information.

* **Parameters:**
  * `ip`: IP address or host of the iLO server.
  * `username`: User with read permissions.
  * `password`: User password.
* **Return:** `IloMetrics` object with parsed data and overall health details.

---

### Infrastructure Adapter `IloRedfishAdapter`
Concrete implementation of `IIloService` to interact with HPE iLO v4/v5+ Redfish API.

#### Usage Example

```csharp
using Axl.Base.Ilo.Domain.Ports;
using Axl.Base.Ilo.Infrastructure.Adapters;

// Configure TLS security and certificates (required for iLO)
System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslErrors) => true;
System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072 | System.Net.SecurityProtocolType.Tls;

IIloService iloService = new IloRedfishAdapter();
var metrics = iloService.GetMetrics("192.168.1.100", "Admin", "SecretPass");

Console.WriteLine($"Server Health: {metrics.SystemHealthRollup}");
Console.WriteLine($"Power Consumption: {metrics.PowerWatts} W");
```

---

### Model `IloMetrics`
Class encapsulating collected data:
* **Timestamp**: Collection Timestamp (UTC).
* **ServerIp**: Queried IP or Host.
* **PowerWatts**: Total chassis power consumption in Watts.
* **Psu1Watts / Psu2Watts**: Individual power supply consumption in Watts.
* **VoltageIn**: Recorded input voltage.
* **InletTempC / Cpu1TempC / Cpu2TempC / HdMaxTempC**: CPU, inlet, and maximum disk temperatures in °C.
* **FanAvgPct**: Average fan speed percentage.
* **SystemHealthRollup**: Overall system health status (e.g., OK, Warning, Critical).
* **StorageHealth**: Storage subsystem health status.
* **Processors**: Descriptive list of installed processors and their status.
* **NetworkInterfaces**: Descriptive list of network interfaces and their status.
* **LatestEvents**: Last 5 events recorded in the hardware IML log.
* **RawDetails**: Dictionary containing errors or additional fields extracted during the query.
