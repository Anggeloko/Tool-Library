using NUnit.Framework;
using Axl.Base.Database.Postgres;
using Axl.Base.Interfaces;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class PostgresTests
    {
        [Test]
        public void Postgres_InstanceProperty_ShouldReturnName()
        {
            var db = new Postgres("Host=localhost;Database=Test", "PostgresProd");
            Assert.AreEqual("PostgresProd", db.Instance);
        }

        [Test]
        public void Postgres_ShouldImplementISql()
        {
            var db = new Postgres("Host=localhost;Database=Test");
            Assert.IsTrue(db is ISql);
        }

        [Test]
        public void Postgres_ShouldImplementICheckable()
        {
            var db = new Postgres("Host=localhost;Database=Test");
            Assert.IsTrue(db is ICheckable);
        }
    }
}
