using NUnit.Framework;
using Axl.Base.Json.Legacy;
using Axl.Base.Models;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class JsonLegacyTests
    {
        private LegacyJson _json;

        [SetUp]
        public void SetUp()
        {
            _json = new LegacyJson();
        }

        [Test]
        public void Serialize_SimpleObject_ReturnsValidJson()
        {
            var obj = new { Name = "Test", Value = 123 };
            string result = _json.Serialize(obj);
            Assert.That(result, Contains.Substring("\"Name\":\"Test\""));
            Assert.That(result, Contains.Substring("\"Value\":123"));
        }

        [Test]
        public void Deserialize_ValidJson_ReturnsObject()
        {
            string json = "{\"Name\":\"Test\",\"Value\":123}";
            var result = _json.Deserialize<TestModel>(json);
            Assert.AreEqual("Test", result.Name);
            Assert.AreEqual(123, result.Value);
        }

        public class TestModel
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}

