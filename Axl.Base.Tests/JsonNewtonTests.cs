using System;
using NUnit.Framework;
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
    public class JsonNewtonTests
    {
        private NewtonJson _jsonService;

        [SetUp]
        public void SetUp()
        {
            _jsonService = new NewtonJson();
        }

        [Test]
        public void Serialize_ShouldReturnValidJson()
        {
            var data = new { Name = "Axl", Value = 123 };
            var json = _jsonService.Serialize(data);

            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"Name\":\"Axl\""));
            Assert.IsTrue(json.Contains("\"Value\":123"));
        }

        [Test]
        public void Deserialize_ShouldReturnValidObject()
        {
            var json = "{\"Name\":\"Axl\",\"Value\":123}";
            var result = _jsonService.Deserialize<TestData>(json);

            Assert.IsNotNull(result);
            Assert.AreEqual("Axl", result.Name);
            Assert.AreEqual(123, result.Value);
        }

        private class TestData
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}


