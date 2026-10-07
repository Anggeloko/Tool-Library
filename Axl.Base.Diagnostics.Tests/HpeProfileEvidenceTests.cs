using System.Collections.Generic;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.IloSnmp.Infrastructure.Adapters;
using NUnit.Framework;

namespace Axl.Base.Diagnostics.Tests
{
    [TestFixture]
    public class HpeProfileEvidenceTests
    {
        private const string Thermal = "1.3.6.1.4.1.232.6.2.6.8.1";
        private const string Cpu = "1.3.6.1.4.1.232.1.2.2.1.1";
        private const string IloVersion = "1.3.6.1.4.1.232.9.2.2.2.0";
        private const string RomVersion = "1.3.6.1.4.1.232.1.2.6.1.0";
        private const string Model = "1.3.6.1.4.1.232.2.2.4.2.0";
        private sealed class Agent : ISnmpService
        {
            public readonly Dictionary<string, string> Scalars = new Dictionary<string, string>();
            public readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();
            public Dictionary<string, string> Get(string deviceId, string ip, int port, int Version, List<string> oids, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
            {
                error = ""; var result = new Dictionary<string, string>();
                foreach (var oid in oids) { string value; result[oid] = Scalars.TryGetValue(oid, out value) ? value : "SNMP No-Such-Object"; }
                return result;
            }
            public Dictionary<string, string> Walk(string deviceId, string ip, int port, int Version, string oid, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
            { error = ""; Dictionary<string, string> data; return Tables.TryGetValue(oid, out data) ? data : new Dictionary<string, string>(); }
            public Dictionary<int, int> InterpretIfTypes(Dictionary<string, string> data, out Dictionary<int, ifTypeEl> defs) { defs = null; return null; }
        }
        private static Agent Pozos()
        {
            // Public hardware descriptions and sensor values from VHOST1's 01-Oct walk.
            var agent = new Agent();
            agent.Scalars[IloSnmpAdapter.OidSysObjectId] = "1.3.6.1.4.1.232.9.4.11";
            agent.Scalars[IloVersion] = "2.41";
            agent.Scalars[RomVersion] = "U30 v2.42 (01/23/2021)";
            agent.Scalars[Model] = "ProLiant DL380 Gen10";
            agent.Scalars["1.3.6.1.4.1.232.1.2.2.4.0"] = "2"; // CPU condition, not firmware.
            agent.Tables[Cpu] = new Dictionary<string, string>();
            foreach (var index in new[] { "1", "0" })
            {
                agent.Tables[Cpu][Cpu + ".2." + index] = "0"; // Slot, not CPU name.
                agent.Tables[Cpu][Cpu + ".3." + index] = "Intel(R) Xeon(R) Silver 4110 CPU @ 2.10GHz";
                agent.Tables[Cpu][Cpu + ".4." + index] = "2100";
            }
            agent.Tables[Thermal] = new Dictionary<string, string> {
                { Thermal + ".3.0.1", "11" }, { Thermal + ".4.0.1", "28" },
                { Thermal + ".3.0.2", "6" }, { Thermal + ".4.0.2", "40" },
                { Thermal + ".3.0.3", "6" }, { Thermal + ".4.0.3", "40" },
                { Thermal + ".3.0.23", "3" }, { Thermal + ".4.0.23", "73" }
            };
            agent.Tables["1.3.6.1.4.1.232.6.2.6.7.1"] = new Dictionary<string, string> {
                { "1.3.6.1.4.1.232.6.2.6.7.1.4.0.1", "3" }, // Present, not percent.
                { "1.3.6.1.4.1.232.6.2.6.7.1.9.0.1", "2" }
            };
            return agent;
        }
        private static IloMetrics Read(Agent agent)
        { return new IloSnmpAdapter(agent).GetMetrics("lab-server", "192.0.2.1", version: 3); }

        [Test] public void ConfirmedVasconiaProfilePublishesSixPercentagesAndAverageWithoutInventingRpm()
        {
            var agent = Pozos();
            agent.Scalars[IloVersion] = "3.18";
            agent.Scalars[IloSnmpAdapter.OidSysDescr] = "Integrated Lights-Out 5 3.18 Feb 08 2026";
            const string fans = "1.3.6.1.4.1.232.6.2.6.7.1";
            var values = new[] { 12, 12, 12, 16, 20, 20 };
            for (int i = 0; i < values.Length; i++) agent.Tables[fans][fans + ".6.0." + (i + 1)] = values[i].ToString();
            var result = Read(agent);
            Assert.That(result.FanAvgPct, Is.EqualTo(15.33));
            Assert.That(result.RawDetails["fan_0_6_speed_pct"], Is.EqualTo(20d));
            Assert.That(result.RawDetails["fan_0_6_column6_raw"], Is.EqualTo(20d));
            Assert.That(result.RawDetails.ContainsKey("fan_avg_pct"), Is.False, "Base persists the top-level average once");
            Assert.That(result.RawDetails["FanPercentageSource"], Is.EqualTo("snmp_hpe_ilo5_3.18_column6"));
            Assert.That(result.RawDetails["fan_health"], Is.EqualTo(1d));
            Assert.That(result.RawDetails.ContainsKey("fan_avg_rpm"), Is.False);
        }

        [TestCase("0", 0d)] [TestCase("2", 2d)] [TestCase("100", 100d)]
        public void ConfirmedProfilePreservesRealZeroAndLowPercentage(string value, double expected)
        {
            var agent = Pozos(); agent.Scalars[IloVersion] = "3.18";
            const string fans = "1.3.6.1.4.1.232.6.2.6.7.1";
            agent.Tables[fans][fans + ".6.0.1"] = value;
            Assert.That(Read(agent).FanAvgPct, Is.EqualTo(expected));
        }

        [TestCase("2.41", "Integrated Lights-Out 5 2.41")]
        [TestCase("3.19", "Integrated Lights-Out 5 3.19")]
        [TestCase("3.18", "Hardware: Intel Windows")]
        public void UnconfirmedOrOsAgentNeverInheritsPercentageProfile(string version, string description)
        {
            var agent = Pozos(); agent.Scalars[IloVersion] = version;
            agent.Scalars[IloSnmpAdapter.OidSysDescr] = description;
            const string fans = "1.3.6.1.4.1.232.6.2.6.7.1";
            agent.Tables[fans][fans + ".6.0.1"] = "12";
            Assert.That(Read(agent).FanAvgPct, Is.Null);
        }

        [TestCase("-1")] [TestCase("101")] [TestCase("NaN")] [TestCase("bad")]
        public void InvalidPercentageDoesNotEnterAverage(string invalid)
        {
            var agent = Pozos(); agent.Scalars[IloVersion] = "3.18";
            const string fans = "1.3.6.1.4.1.232.6.2.6.7.1";
            agent.Tables[fans][fans + ".6.0.1"] = "12";
            agent.Tables[fans][fans + ".6.0.2"] = invalid;
            var result = Read(agent);
            Assert.That(result.FanAvgPct, Is.EqualTo(12));
            Assert.That(result.RawDetails.ContainsKey("fan_0_2_speed_pct"), Is.False);
            Assert.That(result.RawDetails.ContainsKey("Error_FanPercentage"), Is.True);
        }

        [Test] public void DiagnosticsSeparateImlFromComponentHealthAndIdentifyTheResponder()
        {
            var agent = Pozos();
            agent.Scalars[IloSnmpAdapter.OidSysDescr] = "Integrated Lights-Out 5 2.41 Mar 08 2021";
            agent.Scalars["1.3.6.1.4.1.232.2.2.2.1.0"] = "LAB-SERIAL";
            agent.Scalars["1.3.6.1.4.1.232.6.1.3.0"] = "4";
            agent.Scalars["1.3.6.1.4.1.232.6.2.11.2.0"] = "4";
            agent.Scalars["1.3.6.1.4.1.232.6.2.6.1.0"] = "2";
            agent.Scalars["1.3.6.1.4.1.232.6.2.6.4.0"] = "2";
            agent.Tables[Thermal][Thermal + ".6.0.1"] = "2";
            agent.Tables[Thermal][Thermal + ".6.0.2"] = "4";
            var result = Read(agent);
            Assert.That(result.RawDetails["SysDescr"], Does.Contain("2.41"));
            Assert.That(result.RawDetails["SerialNumber"], Is.EqualTo("LAB-SERIAL"));
            Assert.That(result.RawDetails["FirmwareSource"], Is.EqualTo("ilo"));
            Assert.That(result.SystemHealthRollup, Is.EqualTo("Critical"));
            Assert.That(result.RawDetails["ImlHealth"], Is.EqualTo("Critical"));
            Assert.That(result.RawDetails["FanHealthStatus"], Is.EqualTo("OK"));
            Assert.That(result.RawDetails["thermal_health"], Is.EqualTo(0d));
            Assert.That(result.RawDetails["temperature_sensor_0_1_health"], Is.EqualTo(1d));
            Assert.That(result.RawDetails["SnmpVersion"], Is.EqualTo("3"));
            Assert.That(result.RawDetails.ContainsKey("ThermalHealth"), Is.True);
        }

        [TestCase("2", "state_or_low_percentage")]
        [TestCase("68", "percentage_candidate")]
        [TestCase("-1", "unrecognized")]
        public void DisputedFanColumnRemainsEvidenceWithoutAssigningPercentOrRpm(string value, string interpretation)
        {
            var agent = Pozos(); const string fans = "1.3.6.1.4.1.232.6.2.6.7.1";
            agent.Tables[fans][fans + ".6.0.1"] = value;
            var result = Read(agent);
            Assert.That(result.RawDetails["FanColumn6Interpretation"], Is.EqualTo(interpretation));
            Assert.That(result.RawDetails["FanRpmStatus"], Is.EqualTo("not_returned"));
            Assert.That(result.FanAvgPct, Is.Null);
            Assert.That(result.RawDetails.ContainsKey("fan_avg_rpm"), Is.False);
            Assert.That(result.RawDetails["fan_0_1_column6_raw"], Is.EqualTo(double.Parse(value)));
        }

        [Test] public void UnsupportedOptionalDiagnosticOidsDoNotCreateAnAlarmOrFakeIdentity()
        {
            var result = Read(Pozos());
            Assert.That(result.RawDetails.ContainsKey("ImlHealth"), Is.False);
            Assert.That(result.RawDetails.ContainsKey("SerialNumber"), Is.False);
            Assert.That(result.RawDetails.Keys, Has.None.StartsWith("Error_"));
        }

        [Test] public void PozosReplayReadsCorrectFirmwareModelAndBothCpuDescriptions()
        {
            var result = Read(Pozos());
            Assert.That(result.FirmwareVersion, Is.EqualTo("2.41"));
            Assert.That(result.RawDetails["SystemRomVersion"], Is.EqualTo("U30 v2.42 (01/23/2021)"));
            Assert.That(result.RawDetails["ServerModel"], Is.EqualTo("ProLiant DL380 Gen10"));
            Assert.That(result.Processors.Count, Is.EqualTo(2));
            Assert.That(result.Processors, Has.All.Contains("Xeon(R) Silver 4110"));
            Assert.That(result.CpuAvgFreqMhz, Is.EqualTo(2100));
            Assert.That(result.FanAvgPct, Is.Null);
        }

        [Test] public void UnsupportedIloVersionFallsBackToRomWithoutUsingCpuCondition()
        {
            var agent = Pozos(); agent.Scalars[IloVersion] = "SNMP No-Such-Object";
            Assert.That(Read(agent).FirmwareVersion, Is.EqualTo("U30 v2.42 (01/23/2021)"));
            agent.Scalars[RomVersion] = "NoSuchInstance";
            Assert.That(Read(agent).FirmwareVersion, Is.EqualTo("Unknown"));
            agent.Scalars[IloSnmpAdapter.OidSysDescr] = "Integrated Lights-Out 5";
            Assert.That(Read(agent).FirmwareVersion, Is.EqualTo("Integrated Lights-Out 5"));
        }

        [Test] public void NumericThermalLocationsIdentifyRegionsWithoutGuessingPhysicalSensorNames()
        {
            var result = Read(Pozos());
            Assert.That(result.RawDetails["temperature_sensor_0_1_location"], Is.EqualTo("Ambient"));
            Assert.That(result.RawDetails["temperature_sensor_0_2_location"], Is.EqualTo("CPU"));
            Assert.That(result.RawDetails["temperature_sensor_0_23_location"], Is.EqualTo("System"));
            Assert.That(result.RawDetails["temperature_sensor_0_2_c"], Is.EqualTo(40d));
            Assert.That(result.InletTempC, Is.Null); Assert.That(result.Cpu1TempC, Is.Null);
            Assert.That(result.HdMaxTempC, Is.Null); // Locale 3 is System, not a drive.
        }

        [TestCase("-99", false)]
        [TestCase("NaN", false)]
        [TestCase("Infinity", false)]
        [TestCase("0", true)]
        [TestCase("-5", true)]
        public void ThermalUnknownSentinelIsOmittedButRealZeroAndNegativeTemperaturesAreKept(string value, bool expected)
        {
            var agent = Pozos(); agent.Tables[Thermal][Thermal + ".4.0.1"] = value;
            Assert.That(Read(agent).RawDetails.ContainsKey("temperature_sensor_0_1_c"), Is.EqualTo(expected));
        }

        [Test] public void ExplicitThermalLabelsStillPopulateSummaryFields()
        {
            var agent = Pozos(); agent.Tables[Thermal][Thermal + ".8.0.1"] = "01-Inlet Ambient";
            agent.Tables[Thermal][Thermal + ".8.0.2"] = "02-CPU 1";
            agent.Tables[Thermal][Thermal + ".8.0.3"] = "03-CPU 2";
            var result = Read(agent);
            Assert.That(result.InletTempC, Is.EqualTo(28)); Assert.That(result.Cpu1TempC, Is.EqualTo(40));
            Assert.That(result.Cpu2TempC, Is.EqualTo(40));
            Assert.That(result.RawDetails["temperature_sensor_0_2_label"], Is.EqualTo("02-CPU 1"));
        }

        [Test] public void UnsupportedCpuNameAndSpeedDoNotInventZeroOrNonFiniteFrequencies()
        {
            var agent = Pozos(); agent.Tables[Cpu][Cpu + ".3.0"] = "SNMP No-Such-Object";
            agent.Tables[Cpu][Cpu + ".4.1"] = "Infinity";
            var result = Read(agent); Assert.That(result.Processors.Count, Is.EqualTo(1));
            Assert.That(result.CpuAvgFreqMhz, Is.Null);
        }
    }
}
