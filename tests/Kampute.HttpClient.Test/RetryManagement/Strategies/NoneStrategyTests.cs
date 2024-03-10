namespace Kampute.HttpClient.Test.RetryManagement.Strategies
{
    using Kampute.HttpClient.RetryManagement.Strategies;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class NoneStrategyTests
    {
        [Test]
        public void Instance_IsNotNull()
        {
            var instance = NoneStrategy.Instance;

            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void Instance_IsSingleton()
        {
            var instance1 = NoneStrategy.Instance;
            var instance2 = NoneStrategy.Instance;

            Assert.That(instance1, Is.SameAs(instance2));
        }

        [TestCase(0u, 0)]
        [TestCase(1u, 5)]
        [TestCase(10u, 20)]
        public void TryGetRetryDelay_ReturnsFalseAndDefaultDelay(uint attempts, int elapsedMs)
        {
            var elapsed = TimeSpan.FromMilliseconds(elapsedMs);

            var result = NoneStrategy.Instance.TryGetRetryDelay(elapsed, attempts, out var delay);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(delay, Is.Default);
            });
        }
    }
}
