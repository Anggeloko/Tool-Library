using System.Collections.Generic;
using Axl.Base.IloSnmp.Infrastructure.Adapters;
using Axl.Base.Interfaces;
using Moq;
using NUnit.Framework;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class IloSnmpAdapterTests
    {
        private Mock<ISnmpService> _mockSnmpService;
        private IIloService _iloSnmpAdapter;

        [SetUp]
        public void Setup()
        {
            _mockSnmpService = new Mock<ISnmpService>();
            _iloSnmpAdapter = new IloSnmpAdapter(_mockSnmpService.Object);
        }

        [Test]
        public void GetMetrics_ShouldParseScalarsAndTablesCorrectly()
        {
            // Arrange
            string getErr = "";
            var scalarData = new Dictionary<string, string>
            {
                { "1.3.6.1.4.1.232.6.1.3.0", "2" },            // OK
                { "1.3.6.1.4.1.232.9.2.2.1.0", "350" },         // 350 Watts
                { "1.3.6.1.4.1.232.9.2.2.3.0", "2" },           // On
                { "1.3.6.1.4.1.232.6.2.14.4.0", "2" },         // Memory OK
                { "1.3.6.1.4.1.232.3.1.3.0", "2" },            // Storage OK
                { "1.3.6.1.4.1.232.1.2.2.4.0", "P89 v2.80" }   // ROM
            };

            string walkErr = "";
            var thermalData = new Dictionary<string, string>
            {
                { "1.3.6.1.4.1.232.6.2.6.8.1.8.1", "01-Inlet Ambient" },
                { "1.3.6.1.4.1.232.6.2.6.8.1.4.1", "22" },
                { "1.3.6.1.4.1.232.6.2.6.8.1.8.2", "02-CPU 1" },
                { "1.3.6.1.4.1.232.6.2.6.8.1.4.2", "45" },
                { "1.3.6.1.4.1.232.6.2.6.8.1.8.3", "03-HD Max" },
                { "1.3.6.1.4.1.232.6.2.6.8.1.4.3", "38" }
            };

            var psuData = new Dictionary<string, string>
            {
                { "1.3.6.1.4.1.232.6.2.9.3.1.4.1", "2" },
                { "1.3.6.1.4.1.232.6.2.9.3.1.4.2", "3" }
            };

            var fanData = new Dictionary<string, string>
            {
                { "1.3.6.1.4.1.232.6.2.6.7.1.12.1", "24" },
                { "1.3.6.1.4.1.232.6.2.6.7.1.12.2", "26" },
                { "1.3.6.1.4.1.232.6.2.6.7.1.9.1", "2" }
            };

            var cpuData = new Dictionary<string, string>
            {
                { "1.3.6.1.4.1.232.1.2.2.1.1.2.1", "Intel Xeon Gold 6242" },
                { "1.3.6.1.4.1.232.1.2.2.1.1.3.1", "2800" }
            };

            var ifData = new Dictionary<string, string>
            {
                { "1.3.6.1.2.1.2.2.1.2.1", "iLO Dedicated LAN Port" },
                { "1.3.6.1.2.1.2.2.1.8.1", "1" }
            };

            _mockSnmpService.Setup(s => s.Get(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<string>>(), out getErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(scalarData);

            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.232.6.2.6.8.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(thermalData);

            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.232.6.2.9.3.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(psuData);

            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.232.6.2.6.7.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(fanData);

            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.232.1.2.2.1.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(cpuData);

            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.2.1.2.2.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(ifData);

            // Act
            var result = _iloSnmpAdapter.GetMetrics(
                "HPE_Server_01", "172.20.150.41", 161, 3,
                "user 1", "pass123", "priv123", "", "SHA1", "DES");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("OK", result.SystemHealthRollup);
            Assert.AreEqual(350, result.PowerWatts);
            Assert.AreEqual("On", result.PowerState);
            Assert.AreEqual("OK", result.DimmHealth);
            Assert.AreEqual(1.0, result.DimmHealthValue);
            Assert.AreEqual("OK", result.StorageHealth);
            Assert.AreEqual(1.0, result.DriveHealth);
            Assert.AreEqual("P89 v2.80", result.FirmwareVersion);

            Assert.AreEqual(22, result.InletTempC);
            Assert.AreEqual(45, result.Cpu1TempC);
            Assert.AreEqual(38, result.HdMaxTempC);

            Assert.IsNull(result.Psu1Watts);
            Assert.AreEqual(0.5, result.RawDetails["psu_health"]);
            Assert.AreEqual(25, result.FanAvgPct);
            Assert.AreEqual(1.0, result.RawDetails["fan_health"]);

            Assert.AreEqual(1, result.Processors.Count);
            StringAssert.Contains("Intel Xeon Gold 6242", result.Processors[0]);
            Assert.AreEqual(2800, result.CpuAvgFreqMhz);

            Assert.AreEqual(1, result.NetworkInterfaces.Count);
            StringAssert.Contains("iLO Dedicated LAN Port", result.NetworkInterfaces[0]);
        }

        [Test]
        public void GetMetrics_ShouldFallbackToDellOids_WhenHpeOidsAreMissing()
        {
            // Arrange - Simula un Dell PowerEdge / iDRAC donde no responden OIDs de HPE (232), sino los de Dell (674) y sysDescr
            string getErr = "";
            var dellScalarData = new Dictionary<string, string>
            {
                { "1.3.6.1.2.1.1.2.0", "1.3.6.1.4.1.674.10892.5" },            // Dell Enterprise OID
                { "1.3.6.1.2.1.1.5.0", "DELL-R740-01" },                        // Hostname
                { "1.3.6.1.4.1.674.10892.5.2.1.0", "3" },                       // Dell ObjectStatusEnum: 3 = OK
                { "1.3.6.1.4.1.674.10892.5.4.600.30.1.6.1", "240" },            // Dell Watts
                { "1.3.6.1.4.1.674.10892.5.4.200.10.1.9.1", "2" },              // Power State: On
                { "1.3.6.1.4.1.674.10892.5.4.1100.50.1.5.1", "3" },            // Memory OK
                { "1.3.6.1.4.1.674.10892.5.5.1.20.130.1.1.38.1", "3" },        // Virtual Disk Rollup OK
                { "1.3.6.1.4.1.674.10892.5.1.1.8.0", "BIOS 2.12.1" }            // Dell BIOS
            };

            string walkErr = "";
            var emptyWalk = new Dictionary<string, string>();
            var ifData = new Dictionary<string, string>
            {
                { "1.3.6.1.2.1.2.2.1.2.1", "Broadcom Gigabit Ethernet" },
                { "1.3.6.1.2.1.2.2.1.8.1", "1" }
            };

            _mockSnmpService.Setup(s => s.Get(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<string>>(), out getErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(dellScalarData);

            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.674.10892.5.4.700.20.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, string> {
                    { "1.3.6.1.4.1.674.10892.5.4.700.20.1.8.1.1", "System Board Inlet" },
                    { "1.3.6.1.4.1.674.10892.5.4.700.20.1.6.1.1", "235" }
                });
            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.674.10892.5.4.600.12.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, string> {
                    { "1.3.6.1.4.1.674.10892.5.4.600.12.1.5.1.1", "3" },
                    { "1.3.6.1.4.1.674.10892.5.4.600.12.1.5.1.2", "5" }
                });
            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.674.10892.5.4.700.12.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, string> {
                    { "1.3.6.1.4.1.674.10892.5.4.700.12.1.5.1.1", "3" },
                    { "1.3.6.1.4.1.674.10892.5.4.700.12.1.6.1.1", "5200" }
                });
            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.674.10892.5.5.1.20.130.4.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, string> {
                    { "1.3.6.1.4.1.674.10892.5.5.1.20.130.4.1.24.1.1", "4" }
                });

            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.2.1.2.2.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(ifData);

            // Act
            var result = _iloSnmpAdapter.GetMetrics(
                "DELL_Server_01", "192.168.1.50", 161, 3,
                "user", "pass", "priv");

            // Assert - Comprobar que resolvió mediante los OIDs de Dell
            Assert.IsNotNull(result);
            Assert.AreEqual("OK", result.SystemHealthRollup);
            Assert.AreEqual(240, result.PowerWatts);
            Assert.AreEqual("On", result.PowerState);
            Assert.AreEqual("OK", result.DimmHealth);
            Assert.AreEqual(1.0, result.DimmHealthValue);
            Assert.AreEqual("Warning", result.StorageHealth);
            Assert.AreEqual(0.5, result.DriveHealth);
            Assert.AreEqual(23.5, result.InletTempC);
            Assert.AreEqual(0.0, result.RawDetails["psu_health"]);
            Assert.AreEqual(1.0, result.RawDetails["fan_health"]);
            Assert.AreEqual(5200.0, result.RawDetails["fan_avg_rpm"]);
            Assert.AreEqual("BIOS 2.12.1", result.FirmwareVersion);
            Assert.AreEqual("Dell", result.RawDetails["Vendor"]);
            Assert.AreEqual("DELL-R740-01", result.RawDetails["SysName"]);
            Assert.AreEqual(1, result.NetworkInterfaces.Count);
        }

        [Test]
        public void GetMetrics_UsesWorstPhysicalDiskStatus_WhenHpeControllerIsHealthy()
        {
            string getErr = "";
            string walkErr = "";
            _mockSnmpService.Setup(s => s.Get(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<string>>(), out getErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, string> {
                    { "1.3.6.1.2.1.1.2.0", "1.3.6.1.4.1.232" },
                    { "1.3.6.1.4.1.232.3.1.3.0", "2" }
                });
            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.4.1.232.3.2.5.1.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, string> {
                    { "1.3.6.1.4.1.232.3.2.5.1.1.6.1.1", "2" },
                    { "1.3.6.1.4.1.232.3.2.5.1.1.6.1.2", "4" }
                });

            var result = _iloSnmpAdapter.GetMetrics("server", "192.0.2.1", 161, 2, comm: "test");

            Assert.AreEqual("Warning", result.StorageHealth);
            Assert.AreEqual(0.5, result.DriveHealth);
            Assert.AreEqual(0.5, result.RawDetails["disk_health"]);
        }

        [Test]
        public void GetMetrics_ShouldFallbackToGenericMib_WhenDeviceIsRouterOrUnknown()
        {
            // Arrange - Simula un Router o Switch genérico (Asus, Cisco, Linux, etc.)
            string getErr = "";
            var genericData = new Dictionary<string, string>
            {
                { "1.3.6.1.2.1.1.1.0", "ASUS RT-AX86U Linux 4.1.51 #1 SMP PREEMPT" },
                { "1.3.6.1.2.1.1.2.0", "1.3.6.1.4.1.8072.3.2.10" },
                { "1.3.6.1.2.1.1.5.0", "RT-AX86U-Router" }
            };

            string walkErr = "";
            var ifData = new Dictionary<string, string>
            {
                { "1.3.6.1.2.1.2.2.1.2.1", "eth0 (WAN)" },
                { "1.3.6.1.2.1.2.2.1.8.1", "1" },
                { "1.3.6.1.2.1.2.2.1.2.2", "eth1 (LAN)" },
                { "1.3.6.1.2.1.2.2.1.8.2", "1" }
            };

            _mockSnmpService.Setup(s => s.Get(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<string>>(), out getErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(genericData);

            _mockSnmpService.Setup(s => s.Walk(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                "1.3.6.1.2.1.2.2.1", out walkErr, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(ifData);

            // Act
            var result = _iloSnmpAdapter.GetMetrics(
                "Router_01", "192.168.50.1", 161, 2, comm: "public");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ASUS RT-AX86U Linux 4.1.51 #1 SMP PREEMPT", result.FirmwareVersion);
            Assert.AreEqual("Generic/Other", result.RawDetails["Vendor"]);
            Assert.AreEqual("RT-AX86U-Router", result.RawDetails["SysName"]);
            Assert.AreEqual(2, result.NetworkInterfaces.Count);
        }
    }
}
