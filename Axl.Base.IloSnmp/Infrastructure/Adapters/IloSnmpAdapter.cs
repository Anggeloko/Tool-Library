using System;
using System.Collections.Generic;
using System.Globalization;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Snmp.Services;

namespace Axl.Base.IloSnmp.Infrastructure.Adapters
{
    /// <summary>
    /// Adaptador de infraestructura para monitoreo de HPE iLO mediante SNMPv3 / v2c / v1.
    /// Implementa el puerto unificado IIloService.
    /// Consulta tablas y OIDs de HPE Enterprise MIBs (CPQHLTH-MIB, CPQHOST-MIB, CPQIDA-MIB, CPQSINFO-MIB).
    /// </summary>
    public class IloSnmpAdapter : IIloService
    {
        private readonly ISnmpService _snmpService;

        // --- OIDs ESTÁNDAR UNIVERSALES (MIB-II / RFC 1213) ---
        public const string OidSysDescr = "1.3.6.1.2.1.1.1.0";
        public const string OidSysObjectId = "1.3.6.1.2.1.1.2.0";
        public const string OidSysUpTime = "1.3.6.1.2.1.1.3.0";
        public const string OidSysName = "1.3.6.1.2.1.1.5.0";

        // --- OIDs CANDIDATOS (CON FALLBACK MULTIMARCA: HPE, DELL, GENERIC) ---
        // Salud Global: 1) HPE ProLiant cpqHeSysStatus, 2) Dell iDRAC globalSystemStatus, 3) Dell Server Administrator
        public static readonly string[] OidCandidatesSysStatus = new[]
        {
            "1.3.6.1.4.1.232.6.1.3.0",
            "1.3.6.1.4.1.674.10892.5.2.1.0",
            "1.3.6.1.4.1.674.10892.1.200.10.1.2.1"
        };

        // Potencia Watts: 1) HPE cpqPwrSmPwrWatts, 2) HPE iLO alt, 3) Dell iDRAC instantaneousPower
        public static readonly string[] OidCandidatesPowerWatts = new[]
        {
            "1.3.6.1.4.1.232.9.2.2.1.0",
            "1.3.6.1.4.1.232.9.2.5.1.0",
            "1.3.6.1.4.1.674.10892.5.4.600.30.1.6.1"
        };

        // Estado Energía (Power State): 1) HPE cpqPwrSmPwrState, 2) Dell systemPowerState
        public static readonly string[] OidCandidatesPowerState = new[]
        {
            "1.3.6.1.4.1.232.9.2.2.3.0",
            "1.3.6.1.4.1.674.10892.5.4.200.10.1.9.1"
        };

        // Salud Memoria (DIMM): 1) HPE cpqHeResilientMemCondition, 2) Dell globalMemoryStatus
        public static readonly string[] OidCandidatesDimmHealth = new[]
        {
            "1.3.6.1.4.1.232.6.2.14.4.0",
            "1.3.6.1.4.1.674.10892.5.4.1100.50.1.5.1",
            "1.3.6.1.4.1.674.10892.5.2.3.0"
        };

        // Salud Controladora / Discos: 1) HPE Smart Array cpqDaCntlrCondition, 2) Dell virtualDiskRollupStatus
        public static readonly string[] OidCandidatesDriveHealth = new[]
        {
            "1.3.6.1.4.1.232.3.1.3.0",
            "1.3.6.1.4.1.674.10892.5.5.1.20.130.1.1.38.1",
            "1.3.6.1.4.1.674.10892.5.2.4.0"
        };

        // Versión Firmware / ROM: 1) HPE cpqSiSysRomVer, 2) Dell systemBIOSVersion, 3) sysDescr universal
        public static readonly string[] OidCandidatesFirmware = new[]
        {
            "1.3.6.1.4.1.232.1.2.2.4.0",
            "1.3.6.1.4.1.674.10892.5.1.1.8.0",
            "1.3.6.1.2.1.1.1.0"
        };

        // Mantener compatibilidad con constantes previas
        private const string OidSysStatus = "1.3.6.1.4.1.232.6.1.3.0";
        private const string OidPowerWatts = "1.3.6.1.4.1.232.9.2.2.1.0";
        private const string OidPowerState = "1.3.6.1.4.1.232.9.2.2.3.0";
        private const string OidDimmHealth = "1.3.6.1.4.1.232.6.2.14.4.0";
        private const string OidDriveHealth = "1.3.6.1.4.1.232.3.1.3.0";
        private const string OidRomFirmware = "1.3.6.1.4.1.232.1.2.2.4.0";

        // --- TABLAS WALK ---
        private const string TableThermal = "1.3.6.1.4.1.232.6.2.6.8.1";            // .4 Celsius, .8 hardware location
        private const string TablePsu = "1.3.6.1.4.1.232.6.2.9.3.1";                // .4 condition, .5 status (not watts)
        private const string TableFans = "1.3.6.1.4.1.232.6.2.6.7.1";               // .9 condition, .12 current speed
        private const string TableHpeDisks = "1.3.6.1.4.1.232.3.2.5.1.1";           // .6 physical drive status
        private const string TableDellThermal = "1.3.6.1.4.1.674.10892.5.4.700.20.1"; // .6 tenths Celsius, .8 location
        private const string TableDellPsu = "1.3.6.1.4.1.674.10892.5.4.600.12.1";    // .5 status
        private const string TableDellFans = "1.3.6.1.4.1.674.10892.5.4.700.12.1";   // .5 status, .6 RPM
        private const string TableDellDisks = "1.3.6.1.4.1.674.10892.5.5.1.20.130.4.1"; // .24 component status
        private const string TableCpu = "1.3.6.1.4.1.232.1.2.2.1.1";                // cpqSiCpuTable (.2 = Name, .3 = Speed, .6 = Status)
        private const string TableIf = "1.3.6.1.2.1.2.2.1";                         // ifEntry (.2 = ifDescr, .8 = ifOperStatus)

        public IloSnmpAdapter(ISnmpService snmpService = null)
        {
            _snmpService = snmpService ?? new SnmpService();
        }

        public IloMetrics GetMetrics(string ip, string username, string password)
        {
            return GetMetrics(ip, ip, 161, 3, username, password, password, "", "SHA1", "DES", 1000);
        }

        public IloMetrics GetMetrics(
            string deviceId,
            string ip,
            int port = 161,
            int version = 3,
            string user = "",
            string password = "",
            string privacy = "",
            string comm = "",
            string authProto = "SHA1",
            string privProto = "DES",
            int timeoutMs = 1000)
        {
            var metrics = new IloMetrics
            {
                ServerIp = ip,
                Timestamp = DateTime.UtcNow,
                SystemHealthRollup = "Unknown",
                DimmHealth = "Unknown",
                StorageHealth = "Unknown"
            };

            // 1. GET ESCALARES CON FALLBACK (HPE, Dell y MIB-II Universal)
            var scalarOids = new List<string>
            {
                OidSysDescr,
                OidSysObjectId,
                OidSysName
            };

            scalarOids.AddRange(OidCandidatesSysStatus);
            scalarOids.AddRange(OidCandidatesPowerWatts);
            scalarOids.AddRange(OidCandidatesPowerState);
            scalarOids.AddRange(OidCandidatesDimmHealth);
            scalarOids.AddRange(OidCandidatesDriveHealth);
            scalarOids.AddRange(OidCandidatesFirmware);

            string getErr;
            var scalarResults = _snmpService.Get(
                deviceId, ip, port, version, scalarOids,
                out getErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (scalarResults != null && scalarResults.Count > 0)
            {
                ParseScalars(scalarResults, metrics);
            }
            else if (!string.IsNullOrEmpty(getErr))
            {
                metrics.RawDetails["Error_GetScalars"] = getErr;
            }

            var sysObjectId = scalarResults == null ? null : TryGetFirstValid(scalarResults, OidSysObjectId);
            var isDell = !string.IsNullOrEmpty(sysObjectId) && sysObjectId.Contains("1.3.6.1.4.1.674");

            // Walk only the vendor's tables; unsupported subtrees can consume the
            // full SNMP timeout on older management controllers.
            var thermalTable = isDell ? TableDellThermal : TableThermal;
            var psuTable = isDell ? TableDellPsu : TablePsu;
            var fanTable = isDell ? TableDellFans : TableFans;

            // 2. WALK TEMPERATURAS
            string walkErr;
            var thermalResults = _snmpService.Walk(
                deviceId, ip, port, version, thermalTable,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (thermalResults != null && thermalResults.Count > 0)
            {
                ParseThermalTable(thermalResults, metrics, isDell);
            }

            // 3. WALK FUENTES DE PODER (PSU Table)
            var psuResults = _snmpService.Walk(
                deviceId, ip, port, version, psuTable,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (psuResults != null && psuResults.Count > 0)
            {
                ParsePsuTable(psuResults, metrics, isDell);
            }

            // 4. WALK VENTILADORES (Fans Table)
            var fanResults = _snmpService.Walk(
                deviceId, ip, port, version, fanTable,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (fanResults != null && fanResults.Count > 0)
            {
                ParseFanTable(fanResults, metrics, isDell);
            }

            var diskResults = _snmpService.Walk(
                deviceId, ip, port, version, isDell ? TableDellDisks : TableHpeDisks,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);
            if (diskResults != null && diskResults.Count > 0)
                ParseDiskTable(diskResults, metrics, isDell);

            // 5. WALK PROCESADORES (CPU Table)
            var cpuResults = _snmpService.Walk(
                deviceId, ip, port, version, TableCpu,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (cpuResults != null && cpuResults.Count > 0)
            {
                ParseCpuTable(cpuResults, metrics);
            }

            // 6. WALK INTERFACES DE RED (ifEntry - MIB-II Universal para Servidores y Routers)
            var ifResults = _snmpService.Walk(
                deviceId, ip, port, version, TableIf,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (ifResults != null && ifResults.Count > 0)
            {
                ParseIfTable(ifResults, metrics);
            }

            return metrics;
        }

        #region Métodos de Parseo

        private void ParseScalars(Dictionary<string, string> data, IloMetrics metrics)
        {
            // Diagnóstico de Vendor / Dispositivo (sysObjectID y sysDescr)
            string sysObj = TryGetFirstValid(data, OidSysObjectId);
            bool isDell = !string.IsNullOrEmpty(sysObj) && sysObj.Contains("1.3.6.1.4.1.674");
            if (!string.IsNullOrEmpty(sysObj))
            {
                metrics.RawDetails["SysObjectId"] = sysObj;
                if (sysObj.Contains("1.3.6.1.4.1.232")) metrics.RawDetails["Vendor"] = "HPE";
                else if (sysObj.Contains("1.3.6.1.4.1.674")) metrics.RawDetails["Vendor"] = "Dell";
                else if (sysObj.Contains("1.3.6.1.4.1.19046")) metrics.RawDetails["Vendor"] = "Lenovo";
                else metrics.RawDetails["Vendor"] = "Generic/Other";
            }

            string sysName = TryGetFirstValid(data, OidSysName);
            if (!string.IsNullOrEmpty(sysName))
            {
                metrics.RawDetails["SysName"] = sysName;
            }

            // 1. Health Rollup con Fallback (1=other, 2=ok, 3=degraded, 4=failed)
            string statusVal = TryGetFirstValid(data, OidCandidatesSysStatus);
            if (!string.IsNullOrEmpty(statusVal))
            {
                metrics.SystemHealthRollup = MapStatusToHealth(statusVal, isDell);
            }

            // 2. Power Watts con Fallback
            string pwrVal = TryGetFirstValid(data, OidCandidatesPowerWatts);
            if (!string.IsNullOrEmpty(pwrVal))
            {
                if (double.TryParse(pwrVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double pwr))
                    metrics.PowerWatts = pwr;
            }

            // 3. Power State con Fallback (1=other, 2=on, 3=off, 4=bouncing)
            string pState = TryGetFirstValid(data, OidCandidatesPowerState);
            if (!string.IsNullOrEmpty(pState))
            {
                if (pState == "2" || pState.Equals("on", StringComparison.OrdinalIgnoreCase)) metrics.PowerState = "On";
                else if (pState == "3" || pState.Equals("off", StringComparison.OrdinalIgnoreCase)) metrics.PowerState = "Off";
                else metrics.PowerState = "Unknown";
            }

            // 4. Memory / DIMM Health con Fallback
            string memCond = TryGetFirstValid(data, OidCandidatesDimmHealth);
            if (!string.IsNullOrEmpty(memCond))
            {
                metrics.DimmHealth = MapStatusToHealth(memCond, isDell);
                metrics.DimmHealthValue = metrics.DimmHealth == "OK" ? 1.0 : (metrics.DimmHealth == "Warning" ? 0.5 : 0.0);
            }

            // 5. Storage Controller / Discos Health con Fallback
            string driveCond = TryGetFirstValid(data, OidCandidatesDriveHealth);
            if (!string.IsNullOrEmpty(driveCond))
            {
                metrics.StorageHealth = MapStatusToHealth(driveCond, isDell);
                metrics.DriveHealth = metrics.StorageHealth == "OK" ? 1.0 : (metrics.StorageHealth == "Warning" ? 0.5 : 0.0);
            }

            // 6. Firmware Version con Fallback
            string romVer = TryGetFirstValid(data, OidCandidatesFirmware);
            if (!string.IsNullOrEmpty(romVer))
            {
                metrics.FirmwareVersion = romVer.Trim();
            }
        }

        private void ParseThermalTable(Dictionary<string, string> data, IloMetrics metrics, bool isDell)
        {
            var names = new Dictionary<string, string>();
            var values = new Dictionary<string, double>();

            string table = isDell ? TableDellThermal : TableThermal;
            string descPrefix = table + ".8.";
            string valPrefix = table + (isDell ? ".6." : ".4.");

            foreach (var kvp in data)
            {
                if (kvp.Key.StartsWith(descPrefix))
                {
                    string index = kvp.Key.Substring(descPrefix.Length);
                    names[index] = kvp.Value;
                }
                else if (kvp.Key.StartsWith(valPrefix))
                {
                    string index = kvp.Key.Substring(valPrefix.Length);
                    if (double.TryParse(kvp.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double tVal))
                    {
                        values[index] = isDell ? tVal / 10.0 : tVal;
                    }
                }
            }

            double maxHdTemp = 0;

            foreach (var idx in values.Keys)
            {
                string sensorName = names.ContainsKey(idx) ? names[idx] : ("Sensor_" + idx);
                double temp = values[idx];
                metrics.RawDetails["temperature_sensor_" + MetricIndex(idx) + "_c"] = temp;

                if (sensorName.IndexOf("Inlet", StringComparison.OrdinalIgnoreCase) >= 0 || sensorName.IndexOf("Ambient", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    metrics.InletTempC = temp;
                }
                else if (sensorName.IndexOf("CPU 1", StringComparison.OrdinalIgnoreCase) >= 0 || sensorName.IndexOf("Proc 1", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    metrics.Cpu1TempC = temp;
                }
                else if (sensorName.IndexOf("CPU 2", StringComparison.OrdinalIgnoreCase) >= 0 || sensorName.IndexOf("Proc 2", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    metrics.Cpu2TempC = temp;
                }
                else if (sensorName.IndexOf("Exhaust", StringComparison.OrdinalIgnoreCase) >= 0 || sensorName.IndexOf("Outlet", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    metrics.ExhaustTempC = temp;
                }
                else if (sensorName.IndexOf("HD Max", StringComparison.OrdinalIgnoreCase) >= 0 || sensorName.IndexOf("Drive", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (temp > maxHdTemp) maxHdTemp = temp;
                }
            }

            if (maxHdTemp > 0)
            {
                metrics.HdMaxTempC = maxHdTemp;
            }
        }

        private void ParsePsuTable(Dictionary<string, string> data, IloMetrics metrics, bool isDell)
        {
            string statusPrefix = (isDell ? TableDellPsu : TablePsu) + (isDell ? ".5." : ".4.");
            double? worst = null;

            foreach (var kvp in data)
            {
                if (kvp.Key.StartsWith(statusPrefix, StringComparison.Ordinal))
                {
                    var health = HealthScore(kvp.Value, isDell);
                    if (!health.HasValue) continue;
                    metrics.RawDetails["psu_" + MetricIndex(kvp.Key.Substring(statusPrefix.Length)) + "_health"] = health.Value;
                    worst = worst.HasValue ? Math.Min(worst.Value, health.Value) : health;
                }
            }
            if (worst.HasValue) metrics.RawDetails["psu_health"] = worst.Value;
        }

        private void ParseFanTable(Dictionary<string, string> data, IloMetrics metrics, bool isDell)
        {
            string table = isDell ? TableDellFans : TableFans;
            string speedPrefix = table + (isDell ? ".6." : ".12.");
            string statusPrefix = table + (isDell ? ".5." : ".9.");
            double totalSpeed = 0;
            int count = 0;
            double? worst = null;

            foreach (var kvp in data)
            {
                if (kvp.Key.StartsWith(speedPrefix, StringComparison.Ordinal))
                {
                    if (double.TryParse(kvp.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double spd))
                    {
                        metrics.RawDetails["fan_" + MetricIndex(kvp.Key.Substring(speedPrefix.Length)) + (isDell ? "_rpm" : "_speed_pct")] = spd;
                        totalSpeed += spd;
                        count++;
                    }
                }
                else if (kvp.Key.StartsWith(statusPrefix, StringComparison.Ordinal))
                {
                    var health = HealthScore(kvp.Value, isDell);
                    if (!health.HasValue) continue;
                    metrics.RawDetails["fan_" + MetricIndex(kvp.Key.Substring(statusPrefix.Length)) + "_health"] = health.Value;
                    worst = worst.HasValue ? Math.Min(worst.Value, health.Value) : health;
                }
            }

            if (count > 0)
            {
                if (isDell) metrics.RawDetails["fan_avg_rpm"] = Math.Round(totalSpeed / count, 2);
                else metrics.FanAvgPct = Math.Round(totalSpeed / count, 2);
            }
            if (worst.HasValue) metrics.RawDetails["fan_health"] = worst.Value;
        }

        private void ParseDiskTable(Dictionary<string, string> data, IloMetrics metrics, bool isDell)
        {
            string statusPrefix = (isDell ? TableDellDisks + ".24." : TableHpeDisks + ".6.");
            double? worst = null;
            foreach (var kvp in data)
            {
                if (!kvp.Key.StartsWith(statusPrefix, StringComparison.Ordinal)) continue;
                double? health;
                if (isDell) health = HealthScore(kvp.Value, true);
                else
                {
                    // HPE physical drive: 2=OK, 3=failed, 4=predictive failure,
                    // 8=SSD wear out, 9=not authenticated.
                    health = kvp.Value == "2" ? 1.0 :
                        (kvp.Value == "4" || kvp.Value == "8" ? 0.5 :
                        (kvp.Value == "3" || kvp.Value == "9" ? 0.0 : (double?)null));
                }
                if (!health.HasValue) continue;
                metrics.RawDetails["disk_" + MetricIndex(kvp.Key.Substring(statusPrefix.Length)) + "_health"] = health.Value;
                worst = worst.HasValue ? Math.Min(worst.Value, health.Value) : health;
            }
            if (worst.HasValue)
            {
                metrics.RawDetails["disk_health"] = worst.Value;
                metrics.DriveHealth = worst.Value;
                metrics.StorageHealth = worst.Value == 1.0 ? "OK" : (worst.Value == 0.5 ? "Warning" : "Critical");
            }
        }

        private static double? HealthScore(string code, bool isDell)
        {
            if (isDell)
                return code == "3" ? 1.0 : (code == "4" ? 0.5 :
                    (code == "5" || code == "6" ? 0.0 : (double?)null));
            return code == "2" ? 1.0 : (code == "3" ? 0.5 :
                (code == "4" ? 0.0 : (double?)null));
        }

        private static string MetricIndex(string index)
        {
            return index.Replace('.', '_');
        }

        private void ParseCpuTable(Dictionary<string, string> data, IloMetrics metrics)
        {
            // OID .2 = cpqSiCpuName
            // OID .3 = cpqSiCpuSpeed (MHz)
            // OID .6 = cpqSiCpuStatus
            var names = new Dictionary<string, string>();
            var speeds = new Dictionary<string, double>();

            string namePrefix = "1.3.6.1.4.1.232.1.2.2.1.1.2.";
            string speedPrefix = "1.3.6.1.4.1.232.1.2.2.1.1.3.";

            foreach (var kvp in data)
            {
                if (kvp.Key.StartsWith(namePrefix))
                {
                    string idx = kvp.Key.Substring(namePrefix.Length);
                    names[idx] = kvp.Value;
                }
                else if (kvp.Key.StartsWith(speedPrefix))
                {
                    string idx = kvp.Key.Substring(speedPrefix.Length);
                    if (double.TryParse(kvp.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double spd))
                    {
                        speeds[idx] = spd;
                    }
                }
            }

            double totalSpeed = 0;
            int count = 0;

            foreach (var idx in names.Keys)
            {
                string cpuName = names[idx];
                double cpuSpeed = speeds.ContainsKey(idx) ? speeds[idx] : 0;
                metrics.Processors.Add(cpuName + (cpuSpeed > 0 ? $" @ {cpuSpeed} MHz" : ""));

                if (cpuSpeed > 0)
                {
                    totalSpeed += cpuSpeed;
                    count++;
                }
            }

            if (count > 0)
            {
                metrics.CpuAvgFreqMhz = totalSpeed / count;
            }
        }

        private void ParseIfTable(Dictionary<string, string> data, IloMetrics metrics)
        {
            // OID .2 = ifDescr
            // OID .8 = ifOperStatus (1=up, 2=down)
            var names = new Dictionary<string, string>();
            var statuses = new Dictionary<string, string>();

            string descPrefix = "1.3.6.1.2.1.2.2.1.2.";
            string statusPrefix = "1.3.6.1.2.1.2.2.1.8.";

            foreach (var kvp in data)
            {
                if (kvp.Key.StartsWith(descPrefix))
                {
                    string idx = kvp.Key.Substring(descPrefix.Length);
                    names[idx] = kvp.Value;
                }
                else if (kvp.Key.StartsWith(statusPrefix))
                {
                    string idx = kvp.Key.Substring(statusPrefix.Length);
                    statuses[idx] = kvp.Value == "1" ? "Up" : "Down";
                }
            }

            foreach (var idx in names.Keys)
            {
                string nicName = names[idx];
                string nicStatus = statuses.ContainsKey(idx) ? statuses[idx] : "Unknown";
                metrics.NetworkInterfaces.Add($"{nicName}: Status {nicStatus}");
            }
        }

        private static string MapConditionToHealth(string conditionCode)
        {
            switch (conditionCode)
            {
                case "2": return "OK";
                case "3": return "Warning";
                case "4": return "Critical";
                default: return "Unknown";
            }
        }

        private static string MapStatusToHealth(string statusCode, bool isDell)
        {
            var score = HealthScore(statusCode, isDell);
            return !score.HasValue ? "Unknown" :
                (score.Value == 1.0 ? "OK" : (score.Value == 0.5 ? "Warning" : "Critical"));
        }

        private static string TryGetFirstValid(Dictionary<string, string> data, params string[] candidateOids)
        {
            if (data == null || candidateOids == null) return null;

            foreach (var oid in candidateOids)
            {
                if (data.TryGetValue(oid, out string val) && !string.IsNullOrWhiteSpace(val))
                {
                    string trimmed = val.Trim();
                    // Ignorar respuestas de error de agentes SNMP
                    if (!trimmed.Equals("NoSuchObject", StringComparison.OrdinalIgnoreCase) &&
                        !trimmed.Equals("NoSuchInstance", StringComparison.OrdinalIgnoreCase) &&
                        !trimmed.Equals("EndOfMibView", StringComparison.OrdinalIgnoreCase) &&
                        !trimmed.Equals("Null", StringComparison.OrdinalIgnoreCase))
                    {
                        return trimmed;
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
