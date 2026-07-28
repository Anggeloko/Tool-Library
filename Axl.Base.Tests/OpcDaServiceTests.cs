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
using Axl.Base.Statics;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class OpcDaServiceTests
    {
        private OpcDaService _opcDaService;

        [SetUp]
        public void SetUp()
        {
            _opcDaService = new OpcDaService();
        }

        [Test]
        public void Read_WhenParametersNull_ShouldReturnError()
        {
            string error;
            var result = _opcDaService.Read("device1", "svr", "127.0.0.1", null, out error);

            Assert.IsNotNull(result);
            Assert.IsTrue(error.Contains("No se proporcionaron par�metros de lectura"));
        }

        [Test]
        public void Read_WhenParametersEmpty_ShouldReturnError()
        {
            string error;
            var result = _opcDaService.Read("device1", "svr", "127.0.0.1", new Dictionary<string, string>(), out error);

            Assert.IsNotNull(result);
            Assert.IsTrue(error.Contains("No se proporcionaron par�metros de lectura"));
        }

        [Test]
        public void Read_WhenLockTimeout_ShouldReturnError()
        {
            string deviceId = "busyDevice";
            int originalTimeout = TrafficControl.MaxWaitTimeMs;
            TrafficControl.MaxWaitTimeMs = 100; // 100ms for testing

            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                TrafficControl.StartExclusiveHeavy(deviceId);
                System.Threading.Thread.Sleep(500); // Hold it long enough
                TrafficControl.StopExclusiveHeavy(deviceId);
            });

            // Wait a bit to ensure the other thread has the lock
            System.Threading.Thread.Sleep(50);

            try
            {
                string error;
                var result = _opcDaService.Read(deviceId, "svr", "127.0.0.1", new Dictionary<string, string> { { "tag1", "val1" } }, out error);

                Assert.IsNotNull(result);
                Assert.IsTrue(error.Contains("ocupado") || error.Contains("Timeout"));
            }
            finally
            {
                TrafficControl.MaxWaitTimeMs = originalTimeout;
                // Wait for the other thread to release
                System.Threading.Thread.Sleep(500); 
            }
        }
        [Test]
        [Explicit("Prueba manual contra Graybox Simulator")]
        public void ManualTest_OpcDaSimulation()
        {
            string myOpcServerName = "Graybox.Simulator.1";
            var tagsParaLeer = new Dictionary<string, string>
            {
                { "Pressure", "bandwidth" },
                { "Temperature", "numeric.saw.double" },
                { "Cycles",    "time.random" }
            };

            Console.WriteLine($"Leyendo de {myOpcServerName}...");
            OPCRes miResultado = _opcDaService.Read("OPCServer", myOpcServerName, "127.0.0.1", tagsParaLeer, out string errorSalida, 2);
            
            if (miResultado.Resultado != null)
            {
                foreach (var obj in miResultado.Resultado)
                    Console.WriteLine($"[OPC DA] {obj.Nombre}: {obj.ValObj} (Quality: {obj.Calidad})");
            }

            Assert.IsEmpty(errorSalida ?? string.Empty, "Error en lectura OPC DA: " + errorSalida);
            Assert.IsNotEmpty(miResultado.Resultado, "No se obtuvieron resultados de OPC DA");
        }
    }
}


