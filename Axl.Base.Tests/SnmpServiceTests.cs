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
using Axl.Base.Core;
using System.Linq;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class SnmpServiceTests
    {
        private SnmpService _snmpService;

        [SetUp]
        public void SetUp()
        {
            _snmpService = new SnmpService();
        }

        [Test]
        public void InterpretIfTypes_ShouldCorrectlyMapOids()
        {
            var data = new Dictionary<string, string>
            {
                { "1.3.6.1.2.1.2.2.1.3.1", "6" }, // Ethernet
                { "1.3.6.1.2.1.2.2.1.3.2", "24" } // Loopback
            };

            Dictionary<int, ifTypeEl> dict;
            var results = _snmpService.InterpretIfTypes(data, out dict);

            Assert.IsNotNull(results);
            Assert.IsTrue(results.ContainsKey(1));
            Assert.AreEqual(6, results[1]);
            Assert.IsTrue(results.ContainsKey(2));
            Assert.AreEqual(24, results[2]);
            
            Assert.IsNotNull(dict);
            Assert.IsTrue(dict.ContainsKey(6));
            Assert.AreEqual("ethernet-csmacd", dict[6].Nombre);
        }

        [Test]
        public void Get_WhenVersionInvalid_ShouldReturnError()
        {
            string error;
            var result = _snmpService.Get("device1", "127.0.0.1", 161, 99, new List<string> { "oid" }, out error);

            Assert.IsNotNull(result);
            Assert.AreEqual("Version no soportada", error);
        }

        [Test]
        [Explicit("Prueba manual contra dispositivos SNMP reales")]
        public void ManualTest_SnmpSimulation()
        {
            // 1. PC Performance
            Console.WriteLine("Ejecutando SnmpPc para localhost...");
            var collector = new SnmpPc(_snmpService);
            Performance perf = collector.GetPerformance("PC-01", "127.0.0.1", 161, 2, "AXL");
            
            var cpuAvg = perf.Processor.FirstOrDefault(p => p.Core == "avg");
            var ramFirst = perf.MemRAM.FirstOrDefault();
            
            Console.WriteLine($"[SNMP PC] Carga CPU: {(cpuAvg != null ? cpuAvg.Load.ToString() : "N/A")}%");
            Console.WriteLine($"[SNMP PC] RAM Uso: {(ramFirst != null ? (ramFirst.Used * 100).ToString("F2") : "N/A")}%");

            // 2. Printer Info (Simulada o Real si est? disponible)
            Console.WriteLine("Ejecutando SnmpPrinter para 192.168.86.27 (V3)...");
            var printerSvc = new SnmpPrinter(_snmpService);
            try {
                var printerInfo = printerSvc.GetInfo("Printer", "192.168.86.27", 161, 3, "EPSON", "axl", "16845008Af", "16845008Af", authProto: "SHA1", privProto: "AES128");
                foreach (var supply in printerInfo.Supplies)
                    Console.WriteLine($"[SNMP Printer] Suministro: {supply.Name}, Nivel: {Math.Round(supply.RemainingPercentage * 100, 2)}%");
            } catch (Exception ex) {
                Console.WriteLine("[SNMP Printer] Error o dispositivo no alcanzable: " + ex.Message);
            }

            // 3. Operaciones directas
            Console.WriteLine("Pruebas directas de Get/Walk...");
            _snmpService.Get("AxlPC", "127.0.0.1", 161, 2, new List<string> { "1.3.6.1.2.1.1.1.0" }, out string er1, "AXL");
            _snmpService.Walk("AxlPC", "127.0.0.1", 161, 2, "1.3.6.1.2.1.25.3.3.1.2", out er1, "AXL");
        }

        public class SnmpDemo
        {
            public string Ip { get; set; } = "192.168.50.1";
            public string User { get; set; } = "Acme";
            public string Comunity { get; set; } = "public";
            public string Auth { get; set; } = "SHA1";
            public string AuthPswd { get; set; } = "16845008Af";
            public string Priv { get; set; } = "AES128";
            public string PrivPswd { get; set; } = "16845008Af";
            public int Version { get; set; } = 3;
            public int Port { get; set; } = 161;
        }

        [Test]
        [Explicit("Prueba manual usando el vector SnmpDemo del router Asus")]
        public void ManualTest_SnmpDemoVector()
        {
            var demo = new SnmpDemo();
            Console.WriteLine("==========================================================================================");
            Console.WriteLine("                         DETALLE DE RESPUESTAS SNMP DEL ROUTER                            ");
            Console.WriteLine("==========================================================================================");

            // 1. OPERACIÓN GET (Consulta de OIDs individuales específicos)
            var getOids = new List<string>
            {
                "1.3.6.1.2.1.1.1.0", // sysDescr
                "1.3.6.1.2.1.1.3.0", // sysUpTime
                "1.3.6.1.2.1.1.4.0", // sysContact
                "1.3.6.1.2.1.1.5.0", // sysName
                "1.3.6.1.2.1.1.6.0"  // sysLocation
            };

            Console.WriteLine("\n--- [OPERACIÓN: GET] (Consulta puntual de OIDs) ---");
            var getRes = _snmpService.Get("Asus_Get", demo.Ip, demo.Port, 2, getOids, out string getErr, demo.Comunity, to: 3000);
            if (getRes != null && getRes.Count > 0)
            {
                foreach (var kvp in getRes)
                {
                    Console.WriteLine($"[GET ] OID: {kvp.Key,-25} => Valor: {kvp.Value}");
                }
            }

            // 2. OPERACIÓN WALK (Recorrido de tablas de interfaces / errores IF-MIB)
            Console.WriteLine("\n--- [OPERACIÓN: WALK] (Recorrido de Nombres de Interfaz: ifDescr - 1.3.6.1.2.1.2.2.1.2) ---");
            var ifDescrWalk = _snmpService.Walk("Asus_Walk_Descr", demo.Ip, demo.Port, 2, "1.3.6.1.2.1.2.2.1.2", out string wErr1, demo.Comunity, to: 3000);
            if (ifDescrWalk != null)
            {
                foreach (var kvp in ifDescrWalk.Take(10))
                {
                    Console.WriteLine($"[WALK] OID: {kvp.Key,-25} => Interfaz: {kvp.Value}");
                }
            }

            Console.WriteLine("\n--- [OPERACIÓN: WALK] (Recorrido de Errores de Entrada: ifInErrors - 1.3.6.1.2.1.2.2.1.14) ---");
            var ifInErrorsWalk = _snmpService.Walk("Asus_Walk_ErrIn", demo.Ip, demo.Port, 2, "1.3.6.1.2.1.2.2.1.14", out string wErr2, demo.Comunity, to: 3000);
            if (ifInErrorsWalk != null)
            {
                foreach (var kvp in ifInErrorsWalk.Take(10))
                {
                    Console.WriteLine($"[WALK] OID: {kvp.Key,-25} => Errores Entrada: {kvp.Value}");
                }
            }
            Console.WriteLine("==========================================================================================");
        }
    }
}


