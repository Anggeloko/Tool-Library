using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Axl.Base.Snmp.Services;
using NUnit.Framework;
using SnmpSharpNet;

namespace Axl.Base.Diagnostics.Tests
{
    [TestFixture]
    public class SnmpWireTests
    {
        [TestCase(1, "repeat")]
        [TestCase(2, "repeat")]
        [TestCase(1, "empty")]
        [TestCase(2, "empty")]
        [TestCase(1, "outside")]
        [TestCase(2, "outside")]
        [TestCase(2, "end")]
        [TestCase(2, "error")]
        [Timeout(10000)]
        public void WalkTerminatesWithoutRetryingNonAdvancingEmptyEndOrErrorResponse(int version, string scenario)
        {
            using (var agent = new UdpClient(0))
            {
                int requests = 0; Exception failure = null;
                agent.Client.ReceiveTimeout = 4000;
                var server = new Thread(() => {
                    try
                    {
                        IPEndPoint peer = null; var bytes = agent.Receive(ref peer); Interlocked.Increment(ref requests);
                        var response = new Pdu(PduType.Response);
                        if (version == 1) { var request = new SnmpV1Packet(); request.decode(bytes, bytes.Length); response.RequestId = request.Pdu.RequestId; }
                        else { var request = new SnmpV2Packet(); request.decode(bytes, bytes.Length); response.RequestId = request.Pdu.RequestId; }
                        if (scenario == "repeat") response.VbList.Add(new Oid("1.3.6.1.2.1.1"), new Integer32(1));
                        else if (scenario == "outside") response.VbList.Add(new Oid("1.3.6.1.2.1.2.1"), new Integer32(1));
                        else if (scenario == "end") response.VbList.Add(new Oid("1.3.6.1.2.1.1.1"), new EndOfMibView());
                        else if (scenario == "error") response.ErrorStatus = 5;
                        if (version == 1) { var packet = new SnmpV1Packet("lab"); packet.Pdu.Set(response); bytes = packet.encode(); }
                        else { var packet = new SnmpV2Packet("lab"); packet.Pdu.Set(response); bytes = packet.encode(); }
                        agent.Send(bytes, bytes.Length, peer);
                    }
                    catch (Exception ex) { failure = ex; }
                }) { IsBackground = true };
                server.Start(); string error;
                var values = new SnmpService().Walk("wire-test", "127.0.0.1", ((IPEndPoint)agent.Client.LocalEndPoint).Port, version, "1.3.6.1.2.1.1", out error, comm: "lab", to: 200);
                server.Join(4500); Assert.That(failure, Is.Null); Assert.That(requests, Is.EqualTo(1)); Assert.That(values, Is.Empty);
                if (scenario == "repeat") Assert.That(error, Does.Contain("did not advance"));
                else if (scenario == "error") Assert.That(error, Is.Not.Empty);
                else Assert.That(error, Is.Empty);
            }
        }

        [TestCase("1.3.6.1.6.3.15.1.1.3.0", "unknownUserName")]
        [TestCase("1.3.6.1.6.3.15.1.1.5.0", "wrongDigest")]
        [TestCase("1.3.6.1.6.3.15.1.1.6.0", "decryptionError")]
        public void ReportsPreserveAuthenticationFailure(string oid, string expected)
        {
            var report = new Pdu(PduType.Report); report.VbList.Add(new Oid(oid), new Counter32(1));
            var message = (string)typeof(SnmpService).GetMethod("ReportError", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { report });
            Assert.That(message, Does.Contain("authentication_failed").And.Contain(expected));
        }

        [Test, Timeout(10000)] public void V3DiscoveryTimeoutAndMissingCredentialsReachCaller()
        {
            using (var agent = new UdpClient(0))
            {
                var service = new SnmpService(); string error;
                service.Get("v3-timeout", "127.0.0.1", ((IPEndPoint)agent.Client.LocalEndPoint).Port, 3, new List<string> { "1.3.6.1.2.1.1.1.0" }, out error, user: "lab", password: "lab-auth", privacy: "lab-priv", to: 50);
                Assert.That(error, Is.Not.Empty);
                service.Walk("v3-invalid", "127.0.0.1", 161, 3, "1.3.6.1.2.1", out error, user: "", to: 50);
                Assert.That(error, Does.Contain("configuration_invalid"));
            }
        }
    }
}
