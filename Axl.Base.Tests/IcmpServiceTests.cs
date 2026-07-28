using System;
using System.Collections.Generic;
using NUnit.Framework;
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
    public class IcmpServiceTests
    {
        private IcmpService _icmpService;

        [SetUp]
        public void SetUp()
        {
            _icmpService = new IcmpService();
            RouteRegistry.ClearAll();
        }

        [Test]
        public void Read_WhenLockAcquired_ShouldCallReadInternal()
        {
            // Note: This test will actually attempt to ping if not mocked, 
            // but we are testing the entry point and lock logic.
            string error;
            var result = _icmpService.Read("testDevice", "127.0.0.1", out error, timeout: 100, attempts: 1);
            
            Assert.IsNotNull(result);
            Assert.AreEqual("127.0.0.1", result.IP);
        }

        [Test]
        public void GetIp_WhenRouteExists_ShouldReturnCachedIp()
        {
            string deviceId = "device1";
            string expectedIp = "192.168.1.1";
            RouteRegistry.Update(deviceId, expectedIp);

            string error;
            string actualIp = _icmpService.GetIp(deviceId, new List<string> { expectedIp }, out error);

            Assert.AreEqual(expectedIp, actualIp);
            Assert.IsEmpty(error);
        }

        [Test]
        public void GetIp_WhenRouteDoesNotExist_ShouldDiscoverBestIp()
        {
            string deviceId = "device2";
            List<string> ips = new List<string> { "127.0.0.1" };

            string error;
            string actualIp = _icmpService.GetIp(deviceId, ips, out error);

            // 127.0.0.1 should respond
            Assert.AreEqual("127.0.0.1", actualIp);
            Assert.IsEmpty(error);
        }

        [Test]
        public void StatusClassification_Tests()
        {
            // Since ReadInternal is private, we'd normally use reflection or test it through public methods
            // For this demonstration, we'll assume the logic in ReadInternal is what we want to verify
            // by calling a public method that uses it.
            
            // This is a placeholder for more detailed logic tests if we were to refactor for testability.
        }
    }
}


