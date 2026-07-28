# Axl.Base.Ilo

Librería de extracción de métricas de hardware a través de la API Redfish de HPE iLO.

## Prerrequisitos
- **Framework:** .NET Framework 4.0.
- **Dependencias:** `System.Web.Extensions` (para serialización JSON nativa).

## Referencia Técnica (API)

### Puerto/Interfaz `IIloService`

#### Métodos

##### `IloMetrics GetMetrics(string ip, string username, string password)`
Realiza consultas a la API Redfish de iLO para recopilar información de energía, temperaturas, procesadores, almacenamiento, interfaces de red y eventos de hardware.

* **Parámetros:**
  * `ip`: Dirección IP o host del servidor iLO.
  * `username`: Usuario con permisos de lectura.
  * `password`: Contraseña del usuario.
* **Retorno:** Objeto `IloMetrics` con los datos parseados y detalles del estado de salud general.

---

### Adaptador de Infraestructura `IloRedfishAdapter`
Implementación concreta de `IIloService` para interactuar con la API Redfish de HPE iLO v4/v5+.

#### Ejemplo de Uso

```csharp
using Axl.Base.Ilo.Domain.Ports;
using Axl.Base.Ilo.Infrastructure.Adapters;

// Configurar seguridad TLS y certificados (necesario para iLO)
System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslErrors) => true;
System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072 | System.Net.SecurityProtocolType.Tls;

IIloService iloService = new IloRedfishAdapter();
var metrics = iloService.GetMetrics("192.168.1.100", "Admin", "SecretPass");

Console.WriteLine($"Salud del Servidor: {metrics.SystemHealthRollup}");
Console.WriteLine($"Consumo Energético: {metrics.PowerWatts} W");
```

---

### Modelo `IloMetrics`
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
