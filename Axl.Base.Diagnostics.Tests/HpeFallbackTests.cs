using System;
using System.Collections.Generic;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.IloSnmp.Infrastructure.Adapters;
using NUnit.Framework;

namespace Axl.Base.Diagnostics.Tests
{
    [TestFixture]
    public class HpeFallbackTests
    {
        private const string Primary = "1.3.6.1.4.1.232.6.2.6.7.1";
        private const string Alternate = "1.3.6.1.4.1.232.6.2.6.6.1";
        private sealed class Agent : ISnmpService
        {
            public string ScalarError = "";
            public string FanError = "";
            public readonly List<string> Calls = new List<string>();
            public readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();
            public Dictionary<string, string> Get(string deviceId, string ip, int port, int Version, List<string> oids, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
            { error = ScalarError; return error.Length == 0 ? new Dictionary<string, string> { { IloSnmpAdapter.OidSysObjectId, "1.3.6.1.4.1.232" } } : new Dictionary<string, string>(); }
            public Dictionary<string, string> Walk(string deviceId, string ip, int port, int Version, string oid, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
            { Calls.Add(oid); error = oid == Primary ? FanError : ""; Dictionary<string, string> values; return Tables.TryGetValue(oid, out values) ? values : new Dictionary<string, string>(); }
            public Dictionary<int, int> InterpretIfTypes(Dictionary<string, string> data, out Dictionary<int, ifTypeEl> defs) { defs = null; return null; }
        }

        [Test] public void AlternateRpmUsesCompositeIndicesAndPreservesPrimaryHealth()
        {
            var agent = new Agent();
            agent.Tables[Primary] = new Dictionary<string, string> { { Primary + ".12.1.1", "-1" }, { Primary + ".9.1.1", "3" } };
            agent.Tables[Alternate] = new Dictionary<string, string> { { Alternate + ".7.1.1", "10000" }, { Alternate + ".7.1.2", "12000" }, { Alternate + ".5.1.1", "2" } };
            var metrics = new IloSnmpAdapter(agent).GetMetrics("server", "192.0.2.1", version: 2);
            Assert.That(metrics.FanAvgPct, Is.Null); Assert.That(metrics.RawDetails["fan_avg_rpm"], Is.EqualTo(11000d));
            Assert.That(metrics.RawDetails["fan_alt_1_1_rpm"], Is.EqualTo(10000d));
            Assert.That(metrics.RawDetails["fan_health"], Is.EqualTo(.5d)); Assert.That(agent.Calls, Does.Contain(Alternate));
        }

        [Test] public void PrimaryRpmAvoidsSecondTableAndDoubleCounting()
        {
            var agent = new Agent(); agent.Tables[Primary] = new Dictionary<string, string> { { Primary + ".12.1.1", "12000" }, { Primary + ".12.1.2", "14000" } };
            agent.Tables[Alternate] = new Dictionary<string, string> { { Alternate + ".7.1.1", "3000" } };
            var metrics = new IloSnmpAdapter(agent).GetMetrics("server", "192.0.2.1", version: 2);
            Assert.That(metrics.RawDetails["fan_avg_rpm"], Is.EqualTo(13000d)); Assert.That(metrics.FanAvgPct, Is.Null);
            Assert.That(agent.Calls, Does.Not.Contain(Alternate));
        }

        [Test] public void ZeroRpmFromFailedFanRemainsValidMeasurement()
        {
            var agent = new Agent(); agent.Tables[Primary] = new Dictionary<string, string> { { Primary + ".12.1.1", "0" }, { Primary + ".9.1.1", "4" } };
            var metrics = new IloSnmpAdapter(agent).GetMetrics("server", "192.0.2.1", version: 2);
            Assert.That(metrics.RawDetails["fan_avg_rpm"], Is.EqualTo(0d)); Assert.That(metrics.RawDetails["fan_health"], Is.EqualTo(0d));
            Assert.That(metrics.FanAvgPct, Is.Null); Assert.That(agent.Calls, Does.Not.Contain(Alternate));
        }

        [Test] public void FailedFanWalkDoesNotTriggerAlternateAndRetainsErrorWithValidContact()
        {
            var agent = new Agent { FanError = "timeout" };
            var metrics = new IloSnmpAdapter(agent).GetMetrics("server", "192.0.2.1", version: 2);
            Assert.That(agent.Calls, Does.Not.Contain(Alternate)); Assert.That(metrics.RawDetails["Error_Walk_" + Primary], Is.EqualTo("timeout"));
            Assert.That(metrics.RawDetails.ContainsKey("SnmpContact"));
        }

        [Test] public void AuthenticationFailureIsPropagatedAndStopsFurtherQueries()
        {
            var agent = new Agent { ScalarError = "authentication_failed: wrongDigest" };
            var metrics = new IloSnmpAdapter(agent).GetMetrics("server", "192.0.2.1");
            Assert.That(agent.Calls, Is.Empty); Assert.That(metrics.RawDetails.ContainsKey("SnmpContact"), Is.False);
            Assert.That(metrics.RawDetails["Error_GetScalars"], Does.Contain("wrongDigest"));
        }
    }
}
