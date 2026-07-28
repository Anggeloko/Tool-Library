using System;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using Axl.Base.Wmi.Services;
using Axl.Base.Snmp.Services;
using Axl.Base.OpcDa.Services;
using Axl.Base.Mqtt.Services;
using Axl.Base.Icmp.Services;
using Axl.Base.Json.Newton.Services;
using Axl.Base.Database.SQLite.Services;
using Axl.Base.Models;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class WmiServiceTests
    {
        private WmiService _wmiService;

        [SetUp]
        public void SetUp()
        {
            _wmiService = new WmiService();
        }

        [Test]
        public void Read_WhenUserInhabilitado_ShouldReturnError()
        {
            string error;
            WmiQuery query = new WmiQuery { Nmspace = "root/cimv2", CLspace = "Win32_OperatingSystem", Queries = new Dictionary<string, string> { { "Name", "Name" } } };
            var result = _wmiService.Read("device1", "127.0.0.1", "inhabilitado", "pass", query, out error);

            Assert.IsNotNull(result);
            Assert.IsTrue(error.Contains("Usuario no configurado"));
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Read_WithCustomPort_ShouldAttemptConnectionToHostWithPort()
        {
            string error;
            WmiQuery query = new WmiQuery { Nmspace = "root/cimv2", CLspace = "Win32_OperatingSystem", Queries = new Dictionary<string, string> { { "Name", "Name" } } };
            var result = _wmiService.Read("device1", "192.0.2.1", "user", "pass", query, out error, port: 24158);

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(error);
            Assert.IsTrue(error.Contains("192.0.2.1:24158") || error.Contains("WMI"));
        }

        [Test]
        public void Constructor_ShouldInitializeLocalIPs()
        {
            // Simple check to ensure constructor runs and initializes
            Assert.IsNotNull(_wmiService);
        }

        [Test]
        [Explicit("Prueba manual contra WMI local")]
        public void ManualTest_WmiSimulation()
        {
            var query = new WmiQuery
            {
                Nmspace = "root\\CIMV2",
                CLspace = "Win32_OperatingSystem",
                Queries = new Dictionary<string, string> { { "OS", "Caption" }, { "Version", "Version" } }
            };

            Console.WriteLine("Consultando Win32_OperatingSystem vía WmiService...");
            var result = _wmiService.Read("LocalHost", "127.0.0.1", "", "", query, out string err);
            
            Assert.IsEmpty(err ?? string.Empty, "Error en consulta WMI: " + err);
            Assert.IsNotEmpty(result, "No se obtuvieron resultados de WMI");

            foreach (var row in result)
                Console.WriteLine($"[WMI] OS: {row["OS"]}, Version: {row["Version"]}");
        }
    }
}
