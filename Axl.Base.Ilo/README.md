# Axl.Base.Ilo

Hardware metrics extraction library via HPE iLO Redfish API.

## Prerequisites
- **Framework:** .NET Framework 4.0.
- **Dependencias:** `System.Web.Extensions` (para serialización JSON nativa).

## Technical Reference (API)

### Port/Interface `IIloService`

#### Methods

##### `IloMetrics GetMetrics(string ip, string username, string password)`
Realiza consultas a la API Redfish de iLO para recopilar información de energía, temperaturas, procesadores, almacenamiento, interfaces de red y eventos de hardware.

* **Parameters:**
  * `ip`: Dirección IP o host del servidor iLO.
  * `username`: Usuario con permisos de lectura.
  * `password`: Contraseña del usuario.
* **Return:** Objeto `IloMetrics` con los datos parseados y detalles del estado de salud general.

---

### Infrastructure Adapter `IloRedfishAdapter`
Implementación concreta de `IIloService` para interactuar con la API Redfish de HPE iLO v4/v5+.

#### Usage Example

```csharp
using Axl.Base.Ilo.Domain.Ports;
using Axl.Base.Ilo.Infrastructure.Adapters;

// Configurar seguridad TLS y certificados (necesario para iLO)
System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslErrors) => true;
System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072 | System.Net.SecurityProtocolType.Tls;

IIloService iloService = new IloRedfishAdapter();
var metrics = iloService.GetMetrics("192.168.1.100", "Admin", "SecretPass");

Console.WriteLine($"Server Health: {metrics.SystemHealthRollup}");
Console.WriteLine($"Power Consumption: {metrics.PowerWatts} W");
```

---

### Model `IloMetrics`
Clase que encapsula los datos recolectados:
* **Timestamp**: Fecha/hora (UTC) de la recolección.
* **ServerIp**: IP o Host consultado.
* **PowerWatts**: Consumo total del chasis en Watts.
* **Psu1Watts / Psu2Watts**: Consumo individual de las fuentes de poder.
* **VoltageIn**: Voltaje de entrada registrado.
* **InletTempC / Cpu1TempC / Cpu2TempC / HdMaxTempC**: Temperaturas de CPU, entrada y máximo de discos en °C.
* **FanAvgPct**: Velocidad promedio de los ventiladores en porcentaje.
* **SystemHealthRollup**: Estado general de salud del sistema (ej. OK, Warning, Critical).
* **StorageHealth**: Estado de salud del subsistema de almacenamiento.
* **Processors**: Listado descriptivo de los procesadores instalados y su estado.
* **NetworkInterfaces**: Listado descriptivo de las interfaces de red y su estado.
* **LatestEvents**: Últimos 5 eventos registrados en el log IML de hardware.
* **RawDetails**: Diccionario con errores o campos adicionales extraídos durante la consulta.
