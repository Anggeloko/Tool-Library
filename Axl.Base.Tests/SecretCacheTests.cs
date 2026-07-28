using System;
using System.Threading;
using NUnit.Framework;
using Axl.Base.Models;
using Axl.Base.Security.Cache.Services;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class SecretCacheTests
    {
        [SetUp]
        [TearDown]
        public void Cleanup()
        {
            SecretCache.Clear();
        }

        [Test]
        public void GetOrAdd_ShouldRetrieveValueFromCallbackAndCacheIt()
        {
            // Arrange
            int callCount = 0;
            string key = "TestKey";
            string secretValue = "MySecret123";

            Func<SecretValue> callback = () =>
            {
                callCount++;
                return new SecretValue(secretValue);
            };

            // Act
            using (var firstResult = SecretCache.GetOrAdd(key, TimeSpan.FromSeconds(10), callback))
            {
                Assert.AreEqual(secretValue, firstResult.GetString());
            }

            using (var secondResult = SecretCache.GetOrAdd(key, TimeSpan.FromSeconds(10), callback))
            {
                Assert.AreEqual(secretValue, secondResult.GetString());
            }

            // Assert
            Assert.AreEqual(1, callCount, "Callback should only be called once because the value is cached.");
        }

        [Test]
        public void GetOrAdd_ExpiredSecret_ShouldCallCallbackAgain()
        {
            // Arrange
            int callCount = 0;
            string key = "ExpiringKey";
            string secretValue = "ExpiringValue";

            Func<SecretValue> callback = () =>
            {
                callCount++;
                return new SecretValue(secretValue);
            };

            // Act
            using (var firstResult = SecretCache.GetOrAdd(key, TimeSpan.FromMilliseconds(50), callback))
            {
                Assert.AreEqual(secretValue, firstResult.GetString());
            }

            // Wait for expiration
            Thread.Sleep(100);

            using (var secondResult = SecretCache.GetOrAdd(key, TimeSpan.FromMilliseconds(50), callback))
            {
                Assert.AreEqual(secretValue, secondResult.GetString());
            }

            // Assert
            Assert.AreEqual(2, callCount, "Callback should be called again because the cache expired.");
        }

        [Test]
        public void Invalidate_ShouldRemoveSecretFromCache()
        {
            // Arrange
            int callCount = 0;
            string key = "InvalidateKey";
            string secretValue = "ValueToInvalidate";

            Func<SecretValue> callback = () =>
            {
                callCount++;
                return new SecretValue(secretValue);
            };

            // Prime cache
            using (var first = SecretCache.GetOrAdd(key, TimeSpan.FromSeconds(10), callback)) { }

            // Act
            SecretCache.Invalidate(key);

            // Fetch again
            using (var second = SecretCache.GetOrAdd(key, TimeSpan.FromSeconds(10), callback)) { }

            // Assert
            Assert.AreEqual(2, callCount, "Callback should be called again because the key was invalidated.");
        }

        [Test]
        public void Clear_ShouldZeroOutAndRemoveAllSecrets()
        {
            // Arrange
            int callCount = 0;
            string secretValue = "ClearValue";

            Func<SecretValue> callback = () =>
            {
                callCount++;
                return new SecretValue(secretValue);
            };

            using (var first = SecretCache.GetOrAdd("Key1", TimeSpan.FromSeconds(10), callback)) { }
            using (var second = SecretCache.GetOrAdd("Key2", TimeSpan.FromSeconds(10), callback)) { }

            // Act
            SecretCache.Clear();

            // Fetch again
            using (var firstAgain = SecretCache.GetOrAdd("Key1", TimeSpan.FromSeconds(10), callback)) { }
            using (var secondAgain = SecretCache.GetOrAdd("Key2", TimeSpan.FromSeconds(10), callback)) { }

            // Assert
            Assert.AreEqual(4, callCount, "Callback should be called again for all keys because cache was cleared.");
        }
    }
}
