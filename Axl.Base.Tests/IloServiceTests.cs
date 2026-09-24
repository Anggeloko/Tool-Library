using System;
using NUnit.Framework;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Ilo.Infrastructure.Adapters;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class IloServiceTests
    {
        private IIloService _iloService;

        [SetUp]
        public void SetUp()
        {
            _iloService = new IloRedfishAdapter();
        }

        [Test]
        public void IloMetrics_DefaultConstructor_ShouldInitializeCollections()
        {
            var metrics = new IloMetrics();

            Assert.IsNotNull(metrics.Processors);
            Assert.IsNotNull(metrics.NetworkInterfaces);
            Assert.IsNotNull(metrics.LatestEvents);
            Assert.IsNotNull(metrics.RawDetails);
            Assert.AreEqual("OK", metrics.SystemHealthRollup);
            Assert.AreEqual("OK", metrics.DimmHealth);
            Assert.AreEqual("OK", metrics.StorageHealth);
            Assert.AreEqual("Unknown", metrics.PowerState);
            Assert.AreEqual("Unknown", metrics.FirmwareVersion);
        }

        [Test]
        public void GetMetrics_WithInvalidOrUnreachableHost_ShouldPopulateErrorsInRawDetails()
        {
            // Petición a un host inexistente/no-resoluble
            string invalidIp = "http://127.0.0.99:9999";
            
            var metrics = _iloService.GetMetrics(invalidIp, "user", "pass");

            Assert.IsNotNull(metrics);
            Assert.AreEqual(invalidIp, metrics.ServerIp);
            
            // Dado que el host no existe, las llamadas a Redfish fallarán y guardarán los errores correspondientes en RawDetails
            Assert.IsTrue(metrics.RawDetails.Count > 0, "Debe haber registro de errores en RawDetails");
            
            // Validar que alguno de los endpoints fallidos esté registrado en RawDetails
            bool hasPowerError = metrics.RawDetails.ContainsKey("Error__redfish_v1_Chassis_1_Power");
            bool hasThermalError = metrics.RawDetails.ContainsKey("Error__redfish_v1_Chassis_1_Thermal");
            
            Assert.IsTrue(hasPowerError || hasThermalError, "Debería contener errores de conexión a los endpoints principales");
        }

        [Test]
        [Explicit("Prueba manual contra servidor iLO Redfish real o simulador local")]
        public void ManualTest_IloRedfishSimulation()
        {
            // Configuración similar a la usada en Program.cs original
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslErrors) => true;
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072 | System.Net.SecurityProtocolType.Tls;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Advertencia TLS 1.2: " + ex.Message);
            }

            string ip = "localhost:8082"; // Ajustar al endpoint iLO real
            string user = "root";
            string password = "root_password";

            Console.WriteLine($"Consultando iLO en {ip}...");
            var metrics = _iloService.GetMetrics(ip, user, password);

            Console.WriteLine($"IP Servidor: {metrics.ServerIp}");
            Console.WriteLine($"Firmware: {metrics.FirmwareVersion}");
            Console.WriteLine($"Salud: {metrics.SystemHealthRollup}");
            Console.WriteLine($"Consumo: {metrics.PowerWatts} W");
            
            Assert.IsNotNull(metrics);
            Assert.AreEqual(ip, metrics.ServerIp);
        }
    }
}
