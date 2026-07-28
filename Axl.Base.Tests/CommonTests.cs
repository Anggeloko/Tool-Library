using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using Axl.Base.Statics;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class CommonTests
    {
        public class TestModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        [Test]
        public void ToDataTable_ShouldConvertListCorrectly()
        {
            // Arrange
            var list = new List<TestModel>
            {
                new TestModel { Id = 1, Name = "Axl", CreatedAt = DateTime.Now },
                new TestModel { Id = 2, Name = "Antigravity", CreatedAt = null }
            };

            // Act
            var dt = list.ToDataTable();

            // Assert
            Assert.AreEqual(2, dt.Rows.Count);
            Assert.AreEqual(3, dt.Columns.Count);
            Assert.AreEqual(1, dt.Rows[0]["Id"]);
            Assert.AreEqual("Axl", dt.Rows[0]["Name"]);
            Assert.AreEqual(DBNull.Value, dt.Rows[1]["CreatedAt"]);
        }

        [Test]
        public void MachineInfo_Hash_ShouldBePersistent()
        {
            // Act
            var hash1 = MachineInfo.Hash(16);
            var hash2 = MachineInfo.Hash(16);

            // Assert
            Assert.IsNotNull(hash1);
            Assert.AreEqual(16, hash1.Length);
            Assert.AreEqual(hash1, hash2);
        }
    }
}
