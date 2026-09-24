# Axl.Base.IloSnmp

Hardware metrics extraction library for HPE iLO (Integrated Lights-Out) via SNMP (v1, v2c and v3) using HPE Enterprise MIBs (CPQHLTH-MIB, CPQHOST-MIB, CPQIDA-MIB, CPQSINFO-MIB).

## Architecture

This library is decoupled and standalone, following Hexagonal Architecture:
* **Port / Interface**: `IIloService` in `Axl.Base.Interfaces` (`Axl.Base.Common`).
* **Infrastructure Adapter**: `IloSnmpAdapter` in `Axl.Base.IloSnmp.Infrastructure.Adapters`.
* **Domain Model**: `IloMetrics` in `Axl.Base.Models` (`Axl.Base.Common`).

## Usage Example (SNMPv3)

```csharp
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.IloSnmp.Infrastructure.Adapters;

IIloService iloSnmp = new IloSnmpAdapter();

IloMetrics metrics = iloSnmp.GetMetrics(
    deviceId: "HPE_Server_01",
    ip: "172.20.150.41",
    port: 161,
    version: 3,
    user: "user 1",
    password: "tu_auth_password",
    privacy: "tu_priv_password",
    comm: "",                       // Context vacío para v3
    authProto: "SHA1",              // SHA1 o MD5
    privProto: "DES",               // DES, AES128, etc.
    timeoutMs: 1000
);

Console.WriteLine($"Health: {metrics.SystemHealthRollup}");
Console.WriteLine($"Power: {metrics.PowerWatts} W (State: {metrics.PowerState})");
Console.WriteLine($"Inlet Temp: {metrics.InletTempC} °C | CPU1 Temp: {metrics.Cpu1TempC} °C");
Console.WriteLine($"Fan Speed: {metrics.FanAvgPct}%");
Console.WriteLine($"Storage Health: {metrics.StorageHealth}");
```

## Usage Example (SNMPv2c)

```csharp
IIloService iloSnmp = new IloSnmpAdapter();

IloMetrics metrics = iloSnmp.GetMetrics(
    deviceId: "HPE_Server_02",
    ip: "172.20.150.42",
    port: 161,
    version: 2,
    comm: "public",
    timeoutMs: 1000
);
```

## Mapped HPE OIDs

| Metric | OID | Description |
| :--- | :--- | :--- |
| `SystemHealthRollup` | `1.3.6.1.4.1.232.6.1.3.0` | `cpqHeSysStatus` (Overall Health) |
| `PowerWatts` | `1.3.6.1.4.1.232.9.2.2.1.0` | `cpqPwrSmPwrWatts` (Chassis Power) |
| `PowerState` | `1.3.6.1.4.1.232.9.2.2.3.0` | `cpqPwrSmPwrState` (Power Status) |
| `DimmHealth` | `1.3.6.1.4.1.232.6.2.14.4.0` | `cpqHeResilientMemCondition` (Memory Condition) |
| `StorageHealth` | `1.3.6.1.4.1.232.3.1.3.0` | `cpqDaCntlrCondition` (Smart Array Controller) |
| `FirmwareVersion` | `1.3.6.1.4.1.232.1.2.2.4.0` | `cpqSiSysRomVer` (ROM / Firmware Version) |
| `InletTempC` / `Cpu1TempC` / `HdMaxTempC` | `1.3.6.1.4.1.232.6.2.6.8.1` | `.4` Celsius, `.8` hardware location; all readings in `RawDetails` |
| `RawDetails.psu_*_health` | `1.3.6.1.4.1.232.6.2.9.3.1.4` | PSU condition; this table does not report actual watts |
| `FanAvgPct` / `RawDetails.fan_*_health` | `1.3.6.1.4.1.232.6.2.6.7.1` | `.12` current speed, `.9` condition |
| `DriveHealth` / `RawDetails.disk_*_health` | `1.3.6.1.4.1.232.3.2.5.1.1.6` | Physical drive status; worst disk wins |
| `Processors` | `1.3.6.1.4.1.232.1.2.2.1.1` | `cpqSiCpuTable` (CPU Table) |
| `NetworkInterfaces` | `1.3.6.1.2.1.2.2.1` | `ifEntry` (Network Interfaces) |

For Dell iDRAC, the adapter selects the matching `.674.10892.5` temperature
(`.4.700.20.1`), PSU (`.4.600.12.1`), cooling (`.4.700.12.1`) and physical
disk (`.5.1.20.130.4.1`) tables. Dell temperature readings are in tenths of a
degree Celsius, and cooling readings are RPM, so `fan_avg_rpm` is recorded in
`RawDetails` rather than mislabeled as a percentage. Health values use 1=OK,
0.5=warning, 0=critical; unknown status is omitted. Per-device readings and
health are also placed in `RawDetails` for persistence by OTMSBase.

OID references: [HPE MIB mapping](https://support.hpe.com/hpesc/public/api/document/a00101456en_us?v=1640817962000),
[Dell PSU table](https://www.dell.com/support/manuals/en-us/openmanage-software-v9.3/snmp_idrac_cmc_9.3_ref_guide/power-supply-table?guid=guid-a7d2a973-edc4-49e1-92a4-cb62cb41d13c&lang=en-us),
[Dell physical disks](https://www.dell.com/support/manuals/en-ca/idrac9-lifecycle-controller-v3.0-series/snmp_idrac_cmc_9.0.1_reference_guide/physical-disk-table?guid=guid-bbcc3e20-a879-40d2-94c8-05bbdc7bd086&lang=en-us).
