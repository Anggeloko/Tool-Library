using NUnit.Framework;
using Moq;
using Axl.Base.Interfaces;
using Axl.Base.Logs.SQL;
using Axl.Base.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class SQLogTests
    {
        private Mock<IJson> _mockJson;
        private Mock<ISql> _mockSql;

        [SetUp]
        public void SetUp()
        {
            _mockJson = new Mock<IJson>();
            _mockSql = new Mock<ISql>();
            
            // Mock Execute to return a finished task
            _mockSql.Setup(s => s.Execute(It.IsAny<string>(), It.IsAny<object>()))
                    .Returns(Task.FromResult(Result<int>.Success(1)));
        }

        [Test]
        public void SQLog_Constructor_CallsExecute()
        {
            var log = new SQLog(_mockJson.Object, _mockSql.Object, "mssql");
            _mockSql.Verify(s => s.Execute(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce());
        }

        [Test]
        public void SQLog_Info_CallsExecuteInsert()
        {
            var log = new SQLog(_mockJson.Object, _mockSql.Object, "mssql");
            log.Info("Test message");
            
            _mockSql.Verify(s => s.Execute(It.Is<string>(sql => sql.Contains("INSERT INTO")), It.IsAny<object>()), Times.Once());
        }
    }
}

