using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Axl.Base.Ilo.Infrastructure.Adapters;
using NUnit.Framework;

namespace Axl.Base.Diagnostics.Tests
{
    [TestFixture]
    public class RedfishAuthenticationTests
    {
        [Test, Timeout(15000)] public void Http401DoesNotFabricateHealthyMetricsOrValidContact()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
            bool stopped = false;
            var server = new Thread(() => {
                while (!Volatile.Read(ref stopped))
                    try
                    {
                        using (var client = listener.AcceptTcpClient())
                        using (var stream = client.GetStream())
                        {
                            var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                            string line; do { line = reader.ReadLine(); } while (!string.IsNullOrEmpty(line));
                            var response = Encoding.ASCII.GetBytes("HTTP/1.1 401 Unauthorized\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                            stream.Write(response, 0, response.Length);
                        }
                    }
                    catch (SocketException) { if (!Volatile.Read(ref stopped)) throw; }
            }) { IsBackground = true };
            server.Start();
            try
            {
                var result = new IloRedfishAdapter().GetMetrics("lab", "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port, user: "fictional-user", password: "fictional-password", timeoutMs: 500);
                Assert.That(result.RawDetails.ContainsKey("RedfishContact"), Is.False);
                Assert.That(result.DimmHealth, Is.EqualTo("Unknown")); Assert.That(result.SystemHealthRollup, Is.EqualTo("Unknown"));
                Assert.That(result.PowerWatts, Is.Null); Assert.That(result.DriveHealth, Is.Null);
                Assert.That(result.RawDetails.Where(x => x.Key.StartsWith("Error_")).Select(x => x.Value.ToString()).All(x => x.Contains("401")), Is.True);
            }
            finally { Volatile.Write(ref stopped, true); listener.Stop(); server.Join(2000); }
        }
    }
}
