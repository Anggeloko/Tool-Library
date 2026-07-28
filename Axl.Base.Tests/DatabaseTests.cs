using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Database.msSQL;
using Axl.Base.Database.MySQL;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class DatabaseTests
    {
        [Test]
        public void msSQL_InstanceProperty_ShouldReturnName()
        {
            var db = new MsSQL("Server=localhost,1433;Database=TestDB;User Id=user;Password=pass;", "ProdDB");
            Assert.AreEqual("ProdDB", db.Instance);
        }

        [Test]
        public void MySQL_InstanceProperty_ShouldReturnName()
        {
            var db = new MySQL("Server=localhost;Port=3306;Database=TestDB;Uid=user;Pwd=pass;", "DevDB");
            Assert.AreEqual("DevDB", db.Instance);
        }

        [Test]
        public async Task SqlCtr_Integration_ShouldWorkWithMocks()
        {
            // Arrange
            var mockSql = new Mock<ISql>();
            var mockCheck = new Mock<ICheckable>();
            
            mockCheck.Setup(c => c.CheckAsync()).ReturnsAsync(Result<bool>.Success(true));
            mockSql.Setup(s => s.Execute(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(Result<int>.Success(1));

            var context = new SqlCtr(mockSql.Object, mockCheck.Object);

            // Act
            var health = await context.Check.CheckAsync();
            var exec = await context.Sql.Execute("UPDATE Table SET Col = 1", new { Id = 1 });

            // Assert
            Assert.IsTrue(health.IsSuccess);
            Assert.IsTrue(exec.IsSuccess);
            Assert.AreEqual(1, exec.Value);
        }

        [Test]
        public async Task ISql_Execute_DictionaryOverload_ShouldBeImplemented()
        {
            // Verificamos que la interfaz y los proveedores acepten el nuevo método
            var mockSql = new Mock<ISql>();
            var dict = new Dictionary<string, object> { { "Id", 1 } };
            
            mockSql.Setup(s => s.Execute(It.IsAny<string>(), dict)).ReturnsAsync(Result<int>.Success(1));

            var result = await mockSql.Object.Execute("DELETE FROM Table WHERE Id = @Id", dict);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, result.Value);
        }
        [Test]
        public async Task ISql_Upsert_ShouldCallImplementation()
        {
            // Arrange
            var mockSql = new Mock<ISql>();
            var data = new List<TestRecord> { new TestRecord { Id = 1, Value = "Test" } };
            var keys = new[] { "Id" };
            
            mockSql.Setup(s => s.Upsert(It.IsAny<string>(), data, keys)).ReturnsAsync(Result<int>.Success(1));

            // Act
            var result = await mockSql.Object.Upsert("Table", data, keys);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, result.Value);
        }

        [Test]
        public async Task ISql_GenericBulkInsert_ShouldCallImplementation()
        {
            // Arrange
            var mockSql = new Mock<ISql>();
            var data = new List<TestRecord> { new TestRecord { Id = 1, Value = "Test" } };

            mockSql.Setup(s => s.BulkInsert(It.IsAny<string>(), data)).ReturnsAsync(Result<int>.Success(1));

            // Act
            var result = await mockSql.Object.BulkInsert("Table", data);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, result.Value);
        }

        [Test]
        public async Task ISql_PrepareAndBulkInsert_ShouldCallImplementation()
        {
            // Arrange
            var mockSql = new Mock<ISql>();
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Value", typeof(string));

            mockSql.Setup(s => s.PrepareAndBulkInsert(It.IsAny<string>(), dt, true)).ReturnsAsync(Result<int>.Success(2));

            // Act
            var result = await mockSql.Object.PrepareAndBulkInsert("Table", dt, true);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.Value);
        }

        public class TestRecord
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }
    }
}

