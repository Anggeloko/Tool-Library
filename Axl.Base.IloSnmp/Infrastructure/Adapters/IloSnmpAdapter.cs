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
        private const string OidHpeIloFirmware = "1.3.6.1.4.1.232.9.2.2.2.0";
        private const string OidHpeSystemRom = "1.3.6.1.4.1.232.1.2.6.1.0";
        private const string OidHpeServerModel = "1.3.6.1.4.1.232.2.2.4.2.0";
        private const string OidHpeSerial = "1.3.6.1.4.1.232.2.2.2.1.0";
        private const string OidHpeImlHealth = "1.3.6.1.4.1.232.6.2.11.2.0";
        private const string OidHpeThermalHealth = "1.3.6.1.4.1.232.6.2.6.1.0";
        private const string OidHpeFanHealth = "1.3.6.1.4.1.232.6.2.6.4.0";

        // --- OIDs CANDIDATOS (CON FALLBACK MULTIMARCA: HPE, DELL, GENERIC) ---
        // Salud Global: 1) HPE cpqHeMibCondition, 2) Dell iDRAC globalSystemStatus, 3) Dell Server Administrator
        public static readonly string[] OidCandidatesSysStatus = new[]
        {
            "1.3.6.1.4.1.232.6.1.3.0",
            "1.3.6.1.4.1.674.10892.5.2.1.0",
            "1.3.6.1.4.1.674.10892.1.200.10.1.2.1"
        };

        private const string OidPowerMeterSupport = "1.3.6.1.4.1.232.6.2.15.1.0";
        private const string OidPowerMeterStatus = "1.3.6.1.4.1.232.6.2.15.2.0";
        private const string OidPowerWatts = "1.3.6.1.4.1.232.6.2.15.3.0";
        private const string OidPowerState = "1.3.6.1.4.1.232.9.2.2.32.0";

        // CPQHLTH power meter; CPQSM2 ROM date and NIC tables are not power readings.
        public static readonly string[] OidCandidatesPowerWatts = new[]
        {
            OidPowerWatts,
            "1.3.6.1.4.1.674.10892.5.4.600.30.1.6.1"
        };

        // HPE: 2=off, 3=on, 4=power denied; Dell keeps its own enumeration.
        public static readonly string[] OidCandidatesPowerState = new[]
        {
            OidPowerState,
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

        // iLO firmware, system ROM, Dell BIOS and universal description.
        // 232.1.2.2.4.0 is CPU condition, not a firmware version.
        public static readonly string[] OidCandidatesFirmware = new[]
        {
            OidHpeIloFirmware,
            OidHpeSystemRom,
            "1.3.6.1.4.1.674.10892.5.1.1.8.0",
            "1.3.6.1.2.1.1.1.0"
        };

        // Mantener compatibilidad con constantes previas
        private const string OidSysStatus = "1.3.6.1.4.1.232.6.1.3.0";
        private const string OidDimmHealth = "1.3.6.1.4.1.232.6.2.14.4.0";
        private const string OidDriveHealth = "1.3.6.1.4.1.232.3.1.3.0";

        // --- TABLAS WALK ---
        private const string TableThermal = "1.3.6.1.4.1.232.6.2.6.8.1";            // .4 Celsius, .8 hardware location
        private const string TablePsu = "1.3.6.1.4.1.232.6.2.9.3.1"; // .4 health, .6 volts, .7 used watts, .8 maximum watts
        private const string TableFans = "1.3.6.1.4.1.232.6.2.6.7.1";               // .9 condition, .12 current speed
        private const string TableLegacyFans = "1.3.6.1.4.1.232.6.2.6.6.1"; // .5 condition, .7 RPM
        private const string TableHpeDisks = "1.3.6.1.4.1.232.3.2.5.1.1";           // .6 physical drive status
        private const string TableDellThermal = "1.3.6.1.4.1.674.10892.5.4.700.20.1"; // .6 tenths Celsius, .8 location
        private const string TableDellPsu = "1.3.6.1.4.1.674.10892.5.4.600.12.1";    // .5 status
        private const string TableDellFans = "1.3.6.1.4.1.674.10892.5.4.700.12.1";   // .5 status, .6 RPM
        private const string TableDellDisks = "1.3.6.1.4.1.674.10892.5.5.1.20.130.4.1"; // .24 component status
        private const string TableCpu = "1.3.6.1.4.1.232.1.2.2.1.1"; // CPQSTDEQ .3 name, .4 MHz, .6 status
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
            metrics.RawDetails["SnmpVersion"] = version.ToString(CultureInfo.InvariantCulture);
            if (version == 3)
            {
                metrics.RawDetails["SnmpAuthProtocol"] = authProto;
                metrics.RawDetails["SnmpPrivacyProtocol"] = privProto;
            }

            // 1. GET ESCALARES CON FALLBACK (HPE, Dell y MIB-II Universal)
            var scalarOids = new List<string>
            {
                OidSysDescr,
                OidSysObjectId,
                OidSysName
            };

            scalarOids.AddRange(OidCandidatesSysStatus);
            scalarOids.AddRange(OidCandidatesPowerWatts);
            scalarOids.Add(OidPowerMeterSupport);
            scalarOids.Add(OidPowerMeterStatus);
            scalarOids.AddRange(OidCandidatesPowerState);
            scalarOids.AddRange(OidCandidatesDimmHealth);
            scalarOids.AddRange(OidCandidatesDriveHealth);
            scalarOids.AddRange(OidCandidatesFirmware);
            scalarOids.Add(OidHpeServerModel);
            scalarOids.Add(OidHpeSerial);
            scalarOids.Add(OidHpeImlHealth);
            scalarOids.Add(OidHpeThermalHealth);
            scalarOids.Add(OidHpeFanHealth);

            string getErr;
            var reader = new DiagnosticSnmpService(_snmpService, metrics);
            var scalarResults = reader.Get(
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
            var thermalResults = reader.Walk(
                deviceId, ip, port, version, thermalTable,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (thermalResults != null && thermalResults.Count > 0)
            {
                ParseThermalTable(thermalResults, metrics, isDell);
            }

            // 3. WALK FUENTES DE PODER (PSU Table)
            var psuResults = reader.Walk(
                deviceId, ip, port, version, psuTable,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (psuResults != null && psuResults.Count > 0)
            {
                ParsePsuTable(psuResults, metrics, isDell);
            }

            // 4. WALK VENTILADORES (Fans Table)
            var fanResults = reader.Walk(
                deviceId, ip, port, version, fanTable,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (fanResults != null && fanResults.Count > 0)
            {
                ParseFanTable(fanResults, metrics, isDell);
            }

            // The alternative table is queried only after a valid primary response without RPM.
            if (!isDell && string.IsNullOrEmpty(walkErr) && metrics.RawDetails.ContainsKey("SnmpContact") && !metrics.RawDetails.ContainsKey("fan_avg_rpm"))
            {
                var alternate = reader.Walk(deviceId, ip, port, version, TableLegacyFans,
                    out walkErr, comm, user, password, privacy, timeoutMs, false, authProto, privProto);
                if (alternate != null && alternate.Count > 0) ParseFanTable(alternate, metrics, false, true);
            }

            var diskResults = reader.Walk(
                deviceId, ip, port, version, isDell ? TableDellDisks : TableHpeDisks,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);
            if (diskResults != null && diskResults.Count > 0)
                ParseDiskTable(diskResults, metrics, isDell);

            // 5. WALK PROCESADORES (CPU Table)
            var cpuResults = reader.Walk(
                deviceId, ip, port, version, TableCpu,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (cpuResults != null && cpuResults.Count > 0)
            {
                ParseCpuTable(cpuResults, metrics);
            }

            // 6. WALK INTERFACES DE RED (ifEntry - MIB-II Universal para Servidores y Routers)
            var ifResults = reader.Walk(
                deviceId, ip, port, version, TableIf,
                out walkErr, comm, user, password, privacy,
                timeoutMs, false, authProto, privProto);

            if (ifResults != null && ifResults.Count > 0)
            {
                ParseIfTable(ifResults, metrics);
            }

            var fanReadFailed = metrics.RawDetails.ContainsKey("Error_Walk_" + fanTable) ||
                (!isDell && metrics.RawDetails.ContainsKey("Error_Walk_" + TableLegacyFans));
            metrics.RawDetails["FanRpmStatus"] = metrics.RawDetails.ContainsKey("fan_avg_rpm") ? "available" :
                fanReadFailed ? "read_error" : fanResults != null && fanResults.Count > 0 ? "not_returned" : "unknown";
            return metrics;
        }

        // Keep protocol evidence separate from optional, unsupported OIDs. Stop repeated
        // queries after confirmed authentication errors, while preserving earlier valid data.
        private sealed class DiagnosticSnmpService
        {
            private readonly ISnmpService _inner;
            private readonly IloMetrics _metrics;
            private string _fatal;
            public DiagnosticSnmpService(ISnmpService inner, IloMetrics metrics) { _inner = inner; _metrics = metrics; }
            public Dictionary<string, string> Get(string device, string ip, int port, int version, List<string> oids, out string error, string comm, string user, string password, string privacy, int timeout, bool demo, string auth, string priv)
            {
                var result = _inner.Get(device, ip, port, version, oids, out error, comm, user, password, privacy, timeout, demo, auth, priv);
                Observe("GetScalars", result, error);
                return result;
            }
            public Dictionary<string, string> Walk(string device, string ip, int port, int version, string oid, out string error, string comm, string user, string password, string privacy, int timeout, bool demo, string auth, string priv)
            {
                if (_fatal != null) { error = _fatal; return new Dictionary<string, string>(); }
                var result = _inner.Walk(device, ip, port, version, oid, out error, comm, user, password, privacy, timeout, demo, auth, priv);
                Observe("Walk_" + oid, result, error);
                return result;
            }
            private void Observe(string operation, Dictionary<string, string> result, string error)
            {
                if (string.IsNullOrEmpty(error) && result != null) _metrics.RawDetails["SnmpContact"] = true;
                else if (result != null)
                    foreach (var value in result.Values) if (!IsSnmpErrorValue(value)) { _metrics.RawDetails["SnmpContact"] = true; break; }
                if (string.IsNullOrEmpty(error)) return;
                _metrics.RawDetails["Error_" + operation] = error;
                var normalized = error.ToLowerInvariant();
                if (normalized.Contains("authentication") || normalized.Contains("wrongdigest") || normalized.Contains("unknownuser") || normalized.Contains("decryption") || normalized.Contains("password required")) _fatal = error;
            }
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
            CopyScalarDetail(data, metrics, OidSysDescr, "SysDescr");

            // 1. Health Rollup con Fallback (1=other, 2=ok, 3=degraded, 4=failed)
            string statusVal = TryGetFirstValid(data, OidCandidatesSysStatus);
            if (!string.IsNullOrEmpty(statusVal))
            {
                metrics.SystemHealthRollup = MapStatusToHealth(statusVal, isDell);
            }

            ParsePowerScalars(data, metrics, isDell);

            // 4. Memory / DIMM Health con Fallback
            string memCond = TryGetFirstValid(data, OidCandidatesDimmHealth);
            if (!string.IsNullOrEmpty(memCond))
            {
                metrics.DimmHealth = MapStatusToHealth(memCond, isDell);
                metrics.DimmHealthValue = HealthValue(metrics.DimmHealth);
            }

            // 5. Storage Controller / Discos Health con Fallback
            string driveCond = TryGetFirstValid(data, OidCandidatesDriveHealth);
            if (!string.IsNullOrEmpty(driveCond))
            {
                metrics.StorageHealth = MapStatusToHealth(driveCond, isDell);
                metrics.DriveHealth = HealthValue(metrics.StorageHealth);
            }

            // 6. Firmware Version con Fallback
            string romVer = isDell ? TryGetFirstValid(data, "1.3.6.1.4.1.674.10892.5.1.1.8.0", OidSysDescr) :
                TryGetFirstValid(data, OidHpeIloFirmware, OidHpeSystemRom, OidSysDescr);
            if (!string.IsNullOrEmpty(romVer))
            {
                metrics.FirmwareVersion = romVer.Trim();
            }
            if (!isDell)
            {
                CopyScalarDetail(data, metrics, OidHpeIloFirmware, "IloFirmwareVersion");
                CopyScalarDetail(data, metrics, OidHpeSystemRom, "SystemRomVersion");
                CopyScalarDetail(data, metrics, OidHpeServerModel, "ServerModel");
                CopyScalarDetail(data, metrics, OidHpeSerial, "SerialNumber");
                foreach (var health in new[] { new KeyValuePair<string, string>(OidHpeImlHealth, "ImlHealth"),
                    new KeyValuePair<string, string>(OidHpeThermalHealth, "ThermalHealth"),
                    new KeyValuePair<string, string>(OidHpeFanHealth, "FanHealthStatus") })
                {
                    var value = TryGetFirstValid(data, health.Key);
                    if (value != null) metrics.RawDetails[health.Value] = MapStatusToHealth(value, false);
                }
            }
            metrics.RawDetails["FirmwareSource"] = TryGetFirstValid(data, isDell ? "1.3.6.1.4.1.674.10892.5.1.1.8.0" : OidHpeIloFirmware) != null ?
                (isDell ? "bios" : "ilo") : !isDell && TryGetFirstValid(data, OidHpeSystemRom) != null ? "system_rom" :
                TryGetFirstValid(data, OidSysDescr) != null ? "sys_descr" : "unknown";
        }

        private static void CopyScalarDetail(Dictionary<string, string> data, IloMetrics metrics, string oid, string key)
        {
            string value = TryGetFirstValid(data, oid);
            if (value != null) metrics.RawDetails[key] = value;
        }

        private static void ParsePowerScalars(Dictionary<string, string> data, IloMetrics metrics, bool isDell)
        {
            string support = TryGetFirstValid(data, OidPowerMeterSupport);
            string available = TryGetFirstValid(data, OidPowerMeterStatus);
            if (!isDell)
            {
                metrics.RawDetails["PowerMeterSupport"] = support == "2" ? "supported" : support == "3" ? "unsupported" : "unknown";
                metrics.RawDetails["PowerMeterStatus"] = available == "2" ? "present" : available == "3" ? "absent" : "unknown";
            }

            double watts;
            string powerOid = isDell ? OidCandidatesPowerWatts[1] : OidPowerWatts;
            // Pozos returns 0 when status=absent. Do not invent a measured zero or
            // substitute the sum of PSU used capacity for the server power meter.
            if ((isDell || (support == "2" && available == "2")) &&
                TryNonNegativeMeasurement(TryGetFirstValid(data, powerOid), out watts))
                metrics.PowerWatts = watts;

            string state = TryGetFirstValid(data, isDell ? OidCandidatesPowerState[1] : OidPowerState);
            if (state == (isDell ? "2" : "3") || string.Equals(state, "on", StringComparison.OrdinalIgnoreCase))
                metrics.PowerState = "On";
            else if (state == (isDell ? "3" : "2") || string.Equals(state, "off", StringComparison.OrdinalIgnoreCase))
                metrics.PowerState = "Off";
            else
                metrics.PowerState = "Unknown";

            if (!isDell && state == "4")
                metrics.RawDetails["PowerStateReason"] = "insufficient_power_or_power_on_denied";
        }

        private static bool TryNonNegativeMeasurement(string value, out double number)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number) &&
                number >= 0 && !double.IsNaN(number) && !double.IsInfinity(number);
        }

        private void ParseThermalTable(Dictionary<string, string> data, IloMetrics metrics, bool isDell)
        {
            var names = new Dictionary<string, string>();
            var values = new Dictionary<string, double>();
            var locales = new Dictionary<string, string>();

            string table = isDell ? TableDellThermal : TableThermal;
            string descPrefix = table + ".8.";
            string valPrefix = table + (isDell ? ".6." : ".4.");
            string localePrefix = table + ".3.";
            string healthPrefix = table + (isDell ? ".5." : ".6.");
            double? worst = null;

            foreach (var kvp in data)
            {
                if (kvp.Key.StartsWith(descPrefix))
                {
                    string index = kvp.Key.Substring(descPrefix.Length);
                    if (!IsSnmpErrorValue(kvp.Value)) names[index] = kvp.Value;
                }
                else if (kvp.Key.StartsWith(valPrefix))
                {
                    string index = kvp.Key.Substring(valPrefix.Length);
                    if (double.TryParse(kvp.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double tVal) &&
                        !double.IsNaN(tVal) && !double.IsInfinity(tVal) && (isDell || tVal != -99))
                    {
                        values[index] = isDell ? tVal / 10.0 : tVal;
                    }
                }
                else if (!isDell && kvp.Key.StartsWith(localePrefix, StringComparison.Ordinal))
                {
                    locales[kvp.Key.Substring(localePrefix.Length)] = kvp.Value;
                }
                else if (kvp.Key.StartsWith(healthPrefix, StringComparison.Ordinal))
                {
                    var health = HealthScore(kvp.Value, isDell);
                    if (!health.HasValue) continue;
                    metrics.RawDetails["temperature_sensor_" + MetricIndex(kvp.Key.Substring(healthPrefix.Length)) + "_health"] = health.Value;
                    worst = worst.HasValue ? Math.Min(worst.Value, health.Value) : health;
                }
            }
            if (worst.HasValue) metrics.RawDetails["thermal_health"] = worst.Value;

            double maxHdTemp = 0;

            foreach (var idx in values.Keys)
            {
                string sensorName = names.ContainsKey(idx) ? names[idx] : ("Sensor_" + idx);
                double temp = values[idx];
                metrics.RawDetails["temperature_sensor_" + MetricIndex(idx) + "_c"] = temp;
                if (!isDell && locales.ContainsKey(idx))
                    metrics.RawDetails["temperature_sensor_" + MetricIndex(idx) + "_location"] = HpeTemperatureLocation(locales[idx]);
                if (names.ContainsKey(idx))
                    metrics.RawDetails["temperature_sensor_" + MetricIndex(idx) + "_label"] = sensorName;

                // Locale identifies a region, not a CPU number or an inlet/exhaust.
                // Only explicit hardware labels assign the existing summary fields.

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

        private static string HpeTemperatureLocation(string code)
        {
            switch (code)
            {
                case "3": return "System";
                case "4": return "SystemBoard";
                case "5": return "IOBoard";
                case "6": return "CPU";
                case "7": return "Memory";
                case "8": return "Storage";
                case "9": return "RemovableMedia";
                case "10": return "PowerSupply";
                case "11": return "Ambient";
                case "12": return "Chassis";
                case "13": return "BridgeCard";
                default: return "Unknown";
            }
        }

        private void ParsePsuTable(Dictionary<string, string> data, IloMetrics metrics, bool isDell)
        {
            string statusPrefix = (isDell ? TableDellPsu : TablePsu) + (isDell ? ".5." : ".4.");
            double? worst = null;

            foreach (var kvp in data)
            {
                if (!isDell)
                {
                    string suffix = null;
                    string index = null;
                    foreach (var column in new[] { "6", "7", "8" })
                    {
                        string prefix = TablePsu + "." + column + ".";
                        if (!kvp.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                        index = kvp.Key.Substring(prefix.Length);
                        suffix = column == "6" ? "input_volts" : column == "7" ? "used_watts" : "capacity_watts";
                        break;
                    }
                    double measurement;
                    if (suffix != null && TryNonNegativeMeasurement(kvp.Value, out measurement))
                    {
                        metrics.RawDetails["psu_" + MetricIndex(index) + "_" + suffix] = measurement;
                        // Existing Base fields use instantaneous PSU watts, never averages.
                        // Only chassis 0 slots 1/2 have unambiguous legacy field identities.
                        if (suffix == "used_watts" && index == "0.1") metrics.Psu1Watts = measurement;
                        if (suffix == "used_watts" && index == "0.2") metrics.Psu2Watts = measurement;
                    }
                }
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

        private void ParseFanTable(Dictionary<string, string> data, IloMetrics metrics, bool isDell, bool alternate = false)
        {
            string table = isDell ? TableDellFans : alternate ? TableLegacyFans : TableFans;
            string speedPrefix = table + (isDell ? ".6." : alternate ? ".7." : ".12.");
            string statusPrefix = table + (isDell || alternate ? ".5." : ".9.");
            double totalSpeed = 0;
            int count = 0;
            double? worst = null;

            // Retain the disputed .6 column as evidence only. Values 1..3 may
            // be legacy enums or a low percentage; firmware alone is not proof.
            if (!isDell && !alternate)
            {
                var column6 = new List<double>();
                foreach (var entry in data)
                {
                    var prefix = table + ".6.";
                    double value;
                    if (entry.Key.StartsWith(prefix, StringComparison.Ordinal) && double.TryParse(entry.Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value))
                    {
                        column6.Add(value);
                        metrics.RawDetails["fan_" + MetricIndex(entry.Key.Substring(prefix.Length)) + "_column6_raw"] = value;
                    }
                }
                bool enumsOnly = column6.Count > 0 && column6.TrueForAll(v => v == 1 || v == 2 || v == 3);
                metrics.RawDetails["FanColumn6Interpretation"] = column6.Count == 0 ? "not_returned" :
                    enumsOnly ? "state_or_low_percentage" : column6.TrueForAll(v => v >= 0 && v <= 100) ? "percentage_candidate" : "unrecognized";
            }

            foreach (var kvp in data)
            {
                if (kvp.Key.StartsWith(speedPrefix, StringComparison.Ordinal))
                {
                    if (double.TryParse(kvp.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double spd) && spd >= 0 && !double.IsInfinity(spd) && !double.IsNaN(spd))
                    {
                        metrics.RawDetails["fan_" + (alternate ? "alt_" : "") + MetricIndex(kvp.Key.Substring(speedPrefix.Length)) + "_rpm"] = spd;
                        totalSpeed += spd;
                        count++;
                    }
                }
                else if (kvp.Key.StartsWith(statusPrefix, StringComparison.Ordinal))
                {
                    if (alternate && kvp.Value != "2" && kvp.Value != "4") continue;
                    var health = HealthScore(kvp.Value, isDell);
                    if (!health.HasValue) continue;
                    metrics.RawDetails["fan_" + (alternate ? "alt_" : "") + MetricIndex(kvp.Key.Substring(statusPrefix.Length)) + "_health"] = health.Value;
                    worst = worst.HasValue ? Math.Min(worst.Value, health.Value) : health;
                }
            }

            if (count > 0)
            {
                metrics.RawDetails["fan_avg_rpm"] = Math.Round(totalSpeed / count, 2);
            }
            if (worst.HasValue && (!alternate || !metrics.RawDetails.ContainsKey("fan_health"))) metrics.RawDetails["fan_health"] = worst.Value;
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
            // CPQSTDEQ-MIB: .2 is slot, .3 name, .4 MHz, .6 status.
            var names = new Dictionary<string, string>();
            var speeds = new Dictionary<string, double>();

            string namePrefix = TableCpu + ".3.";
            string speedPrefix = TableCpu + ".4.";

            foreach (var kvp in data)
            {
                if (kvp.Key.StartsWith(namePrefix))
                {
                    string idx = kvp.Key.Substring(namePrefix.Length);
                    if (!IsSnmpErrorValue(kvp.Value)) names[idx] = kvp.Value.Trim();
                }
                else if (kvp.Key.StartsWith(speedPrefix))
                {
                    string idx = kvp.Key.Substring(speedPrefix.Length);
                    if (TryNonNegativeMeasurement(kvp.Value, out double spd))
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

        // Valor numerico de una salud ya mapeada. "Unknown" (el agente no dio un codigo que se pueda
        // interpretar) es AUSENCIA de dato, no falla: devuelve null y el Base no guarda la metrica.
        // Antes caia en 0.0, igual que "Critical": los iDRAC Dell que no exponen el OID salian en la
        // pantalla con toda la memoria y todos los discos en falla (medido 2026-09-29: EqCode 68, 69,
        // 105 y 129, todos Dell, dimm_health = drive_health = 0 con DimmHealth "Unknown").
        private static double? HealthValue(string health)
        {
            if (health == "OK") return 1.0;
            if (health == "Warning") return 0.5;
            if (health == "Critical") return 0.0;
            return null;
        }

        // Respuestas de error de un agente SNMP que llegan como texto en lugar de un valor. La libreria
        // SNMP no siempre las entrega con la misma forma: medido en iDRAC Dell el texto es
        // "SNMP No-Such-Object", que el Equals exacto contra "NoSuchObject" dejaba pasar como valor.
        // Se comparan solo las letras, sin espacios, guiones ni prefijo.
        private static readonly string[] SnmpErrorMarkers = { "nosuchobject", "nosuchinstance", "endofmibview" };

        private static bool IsSnmpErrorValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            var letters = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
                if (char.IsLetter(c)) letters.Append(char.ToLowerInvariant(c));
            string norm = letters.ToString();
            if (norm == "null") return true;
            foreach (var marker in SnmpErrorMarkers)
                if (norm.IndexOf(marker, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        private static string TryGetFirstValid(Dictionary<string, string> data, params string[] candidateOids)
        {
            if (data == null || candidateOids == null) return null;

            foreach (var oid in candidateOids)
            {
                if (data.TryGetValue(oid, out string val) && !string.IsNullOrWhiteSpace(val))
                {
                    string trimmed = val.Trim();
                    // Ignorar respuestas de error de agentes SNMP (en cualquiera de sus formas de texto)
                    if (!IsSnmpErrorValue(trimmed))
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
