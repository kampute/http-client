namespace Kampute.HttpClient.Test.Utilities
{
    using Kampute.HttpClient.Utilities;
    using NUnit.Framework;
    using System.Linq;
    using System.Threading.Tasks;

    [TestFixture]
    public class FlyweightCacheTests
    {
        [Test]
        public void Get_WhenKeyDoesNotExist_AddsValue()
        {
            var cache = new FlyweightCache<int, string>(key => $"Value {key}");

            var result = cache.Get(1);

            Assert.That(result, Is.EqualTo("Value 1"));
        }

        [Test]
        public void Get_WhenKeyExists_ReturnsExistingValue()
        {
            var cache = new FlyweightCache<int, object>(key => $"Value {key}");

            var result1 = cache.Get(1);
            var result2 = cache.Get(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cache.Count, Is.EqualTo(1));
                Assert.That(result2, Is.SameAs(result1));
            }
        }

        [Test]
        public async Task Get_IsThreadSafe()
        {
            var cache = new FlyweightCache<int, string>(key => $"Value {key}");

            var tasks = Enumerable.Range(0, 100).Select(i => Task.Factory.StartNew(() => cache.Get(i % 2)));
            var results = await Task.WhenAll(tasks);

            Assert.That(cache.Count, Is.EqualTo(2));

            for (var i = 0; i < results.Length; ++i)
                Assert.That(results[i], Is.EqualTo($"Value {i % 2}"));
        }

        [Test]
        public void Contains_WhenKeyDoesNotExist_ReturnsFalse()
        {
            var cache = new FlyweightCache<int, string>(key => $"Value {key}");

            var result = cache.Contains(1);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Contains_WhenKeyExists_ReturnsTrue()
        {
            var cache = new FlyweightCache<int, string>(key => $"Value {key}");
            var _ = cache.Get(1);

            var result = cache.Contains(1);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Clear_RemovesAllEntries()
        {
            var cache = new FlyweightCache<int, string>(key => $"Value {key}");
            var _ = cache.Get(1);

            cache.Clear();

            Assert.That(cache.Count, Is.Zero);
        }
    }
}
