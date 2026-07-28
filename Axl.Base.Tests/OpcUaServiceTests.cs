using NUnit.Framework;
using Axl.Base.OpcUa;
using Axl.Base.Interfaces;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class OpcUaServiceTests
    {
        [Test]
        public void Constructor_ValidEndpoint_ShouldInitialize()
        {
            var client = new OpcUaClient("opc.tcp://localhost:4840");
            Assert.IsNotNull(client);
        }

        [Test]
        public void Constructor_NullEndpoint_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new OpcUaClient(null));
        }

        [Test]
        public void Dispose_SuccessiveCalls_ShouldNotThrow()
        {
            var client = new OpcUaClient("opc.tcp://localhost:4840");
            client.Dispose();
            Assert.DoesNotThrow(() => client.Dispose());
        }

        [Test]
        [Explicit("Prueba manual contra servidor real")]
        public async Task ManualTest_OpcUaSimulation()
        {
            string endpoint = "opc.tcp://Axl001:53530/OPCUA/SimulationServer";
            Console.WriteLine($"Conectando a {endpoint}...");
            
            using (var client = new OpcUaClient(endpoint, timeout: 5000))
            {
                await client.ConnectAsync();
                Console.WriteLine("¡Conectado!");

                var nodeIds = new List<string> { "ns=3;i=1003", "ns=3;i=1005" };
                var readResult = await client.ReadNodesAsync(nodeIds);
                
                Assert.IsTrue(readResult.IsSuccess, "La lectura de nodos falló: " + string.Join(", ", readResult.Errors));
                
                foreach (var kvp in readResult.Value)
                {
                    Console.WriteLine($"[OPC UA] Node: {kvp.Key}, Value: {kvp.Value}");
                }

                var browseResult = await client.BrowseNodesAsync("ns=3;s=85/0:Simulation");
                Assert.IsTrue(browseResult.IsSuccess, "El browse falló");
                Console.WriteLine($"[OPC UA] Browsed {browseResult.Value.Count} nodes.");
            }
        }
    }
}

