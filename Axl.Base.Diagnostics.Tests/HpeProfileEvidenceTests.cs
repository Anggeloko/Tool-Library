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
