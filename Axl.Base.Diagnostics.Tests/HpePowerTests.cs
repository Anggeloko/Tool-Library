using System;
using System.Collections.Generic;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.IloSnmp.Infrastructure.Adapters;
using NUnit.Framework;

namespace Axl.Base.Diagnostics.Tests
{
    [TestFixture]
    public class HpePowerTests
    {
        private const string Meter = "1.3.6.1.4.1.232.6.2.15.";
        private const string State = "1.3.6.1.4.1.232.9.2.2.32.0";
        private const string Psu = "1.3.6.1.4.1.232.6.2.9.3.1";
        private sealed class Agent : ISnmpService
        {
            public readonly Dictionary<string, string> Scalars = new Dictionary<string, string>();
            public readonly Dictionary<string, string> Supplies = new Dictionary<string, string>();
            public List<string> Requested;
            public Dictionary<string, string> Get(string deviceId, string ip, int port, int Version, List<string> oids, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
            {
                error = ""; Requested = oids;
                var result = new Dictionary<string, string>();
                foreach (var oid in oids)
                {
                    string value;
                    result[oid] = Scalars.TryGetValue(oid, out value) ? value : "SNMP No-Such-Object";
                }
                return result;
            }
            public Dictionary<string, string> Walk(string deviceId, string ip, int port, int Version, string oid, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
            { error = ""; return oid == Psu ? Supplies : new Dictionary<string, string>(); }
            public Dictionary<int, int> InterpretIfTypes(Dictionary<string, string> data, out Dictionary<int, ifTypeEl> defs) { defs = null; return null; }
        }

        private static Agent Pozos()
        {
            // Energy-only replay of VHOST1's authenticated 01-Oct-2026 10:27 walk.
            // No addresses, account names, secrets or IML inventory are embedded.
            var agent = new Agent();
            agent.Scalars[IloSnmpAdapter.OidSysObjectId] = "1.3.6.1.4.1.232.9.4.11";
            agent.Scalars[Meter + "1.0"] = "2";
            agent.Scalars[Meter + "2.0"] = "3";
            agent.Scalars[Meter + "3.0"] = "0";
            agent.Scalars[State] = "3";
            agent.Scalars["1.3.6.1.4.1.232.9.2.2.1.0"] = "03/08/2021";
            agent.Scalars["1.3.6.1.4.1.232.9.2.2.3.0"] = "1";
            foreach (var index in new[] { "0.1", "0.2" })
            {
                agent.Supplies[Psu + ".4." + index] = "2";
                agent.Supplies[Psu + ".5." + index] = "1";
                agent.Supplies[Psu + ".8." + index] = "800";
            }
            agent.Supplies[Psu + ".6.0.1"] = "116";
            agent.Supplies[Psu + ".6.0.2"] = "115";
            agent.Supplies[Psu + ".7.0.1"] = "6";
            agent.Supplies[Psu + ".7.0.2"] = "182";
            return agent;
        }

        private static IloMetrics Read(Agent agent)
        { return new IloSnmpAdapter(agent).GetMetrics("lab-server", "192.0.2.1", version: 3); }

        [Test] public void PozosReplayReadsPsuWattsAndVoltageWithoutInventingTotalOrAverages()
        {
            var agent = Pozos(); var result = Read(agent);
            Assert.That(result.PowerState, Is.EqualTo("On"));
            Assert.That(result.PowerWatts, Is.Null);
            Assert.That(result.Psu1Watts, Is.EqualTo(6)); Assert.That(result.Psu2Watts, Is.EqualTo(182));
            Assert.That(result.Psu1AvgWatts, Is.Null); Assert.That(result.Psu2AvgWatts, Is.Null);
            Assert.That(result.RawDetails["psu_0_1_used_watts"], Is.EqualTo(6d));
            Assert.That(result.RawDetails["psu_0_2_input_volts"], Is.EqualTo(115d));
            Assert.That(result.RawDetails["psu_0_1_capacity_watts"], Is.EqualTo(800d));
            Assert.That(result.RawDetails["psu_health"], Is.EqualTo(1d));
            Assert.That(result.RawDetails["PowerMeterStatus"], Is.EqualTo("absent"));
            Assert.That(result.RawDetails["SnmpContact"], Is.EqualTo(true));
            Assert.That(agent.Requested, Does.Not.Contain("1.3.6.1.4.1.232.9.2.2.1.0"));
            Assert.That(agent.Requested, Does.Not.Contain("1.3.6.1.4.1.232.9.2.2.3.0"));
        }

        [TestCase("2", "2", "188", 188d)]
        [TestCase("2", "2", "0", 0d)]
        [TestCase("2", "3", "188", null)]
        [TestCase("3", "2", "188", null)]
        [TestCase("1", "2", "188", null)]
        [TestCase(null, "2", "188", null)]
        [TestCase("2", null, "188", null)]
        [TestCase("2", "2", "-1", null)]
        [TestCase("2", "2", "NaN", null)]
        [TestCase("2", "2", "Infinity", null)]
        [TestCase("2", "2", "SNMP No-Such-Instance", null)]
        public void GlobalMeterRequiresSupportAvailableDataAndFiniteNonNegativeWatts(string support, string status, string watts, double? expected)
        {
            var agent = Pozos(); agent.Scalars[Meter + "1.0"] = support;
            agent.Scalars[Meter + "2.0"] = status; agent.Scalars[Meter + "3.0"] = watts;
            Assert.That(Read(agent).PowerWatts, Is.EqualTo(expected));
        }

        [TestCase("1", "Unknown")]
        [TestCase("2", "Off")]
        [TestCase("3", "On")]
        [TestCase("4", "Unknown")]
        [TestCase("SNMP No-Such-Object", "Unknown")]
        public void HpePowerStateUsesCpqSm2Enumeration(string state, string expected)
        {
            var agent = Pozos(); agent.Scalars[State] = state;
            var result = Read(agent); Assert.That(result.PowerState, Is.EqualTo(expected));
            if (state == "4") Assert.That(result.RawDetails["PowerStateReason"], Is.EqualTo("insufficient_power_or_power_on_denied"));
        }

        [TestCase("-1")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("SNMP No-Such-Object")]
        public void InvalidPsuReadingsRemainAbsentWithoutLosingValidHealth(string invalid)
        {
            var agent = Pozos();
            foreach (var col in new[] { "6", "7", "8" }) agent.Supplies[Psu + "." + col + ".0.1"] = invalid;
            var result = Read(agent);
            Assert.That(result.Psu1Watts, Is.Null); Assert.That(result.Psu2Watts, Is.EqualTo(182));
            Assert.That(result.RawDetails.ContainsKey("psu_0_1_used_watts"), Is.False);
            Assert.That(result.RawDetails.ContainsKey("psu_0_1_input_volts"), Is.False);
            Assert.That(result.RawDetails.ContainsKey("psu_0_1_capacity_watts"), Is.False);
            Assert.That(result.RawDetails["psu_health"], Is.EqualTo(1d));
        }

        [Test] public void ZeroPsuWattsIsValidAndDoesNotHidePhysicalFailure()
        {
            var agent = Pozos(); agent.Supplies[Psu + ".7.0.1"] = "0";
            agent.Supplies[Psu + ".4.0.1"] = "4";
            var result = Read(agent); Assert.That(result.Psu1Watts, Is.EqualTo(0));
            Assert.That(result.RawDetails["psu_health"], Is.EqualTo(0d));
            Assert.That(result.RawDetails["SnmpContact"], Is.EqualTo(true));
        }

        [Test] public void OtherChassisAndSlotsRemainSeparateAndMissingPsuDoesNotBecomeZero()
        {
            var agent = Pozos(); agent.Supplies.Clear();
            agent.Supplies[Psu + ".7.10.1"] = "90"; agent.Supplies[Psu + ".7.0.10"] = "12";
            var result = Read(agent);
            Assert.That(result.RawDetails["psu_10_1_used_watts"], Is.EqualTo(90d));
            Assert.That(result.RawDetails["psu_0_10_used_watts"], Is.EqualTo(12d));
            Assert.That(result.Psu1Watts, Is.Null); Assert.That(result.Psu2Watts, Is.Null);
        }

        [Test] public void DellKeepsItsOwnWattsAndStateEvenWithConflictingHpeValues()
        {
            var agent = Pozos(); agent.Scalars[IloSnmpAdapter.OidSysObjectId] = "1.3.6.1.4.1.674.10892.5";
            agent.Scalars["1.3.6.1.4.1.674.10892.5.4.600.30.1.6.1"] = "240";
            agent.Scalars["1.3.6.1.4.1.674.10892.5.4.200.10.1.9.1"] = "2";
            var result = Read(agent); Assert.That(result.PowerWatts, Is.EqualTo(240));
            Assert.That(result.PowerState, Is.EqualTo("On")); Assert.That(result.Psu1Watts, Is.Null);
            Assert.That(result.RawDetails.ContainsKey("PowerMeterStatus"), Is.False);
        }
    }
}
