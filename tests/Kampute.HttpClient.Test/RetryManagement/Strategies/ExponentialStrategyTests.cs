namespace Kampute.HttpClient.Test.RetryManagement.Strategies
{
    using Kampute.HttpClient.RetryManagement.Strategies;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class ExponentialStrategyTests
    {
        [Test]
        public void Constructor_WhenRateIsLessThanOne_ThrowsArgumentOutOfRangeException()
        {
            var initialDelay = TimeSpan.FromSeconds(1);
            var rate = 0.5;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialStrategy(initialDelay, rate));

            Assert.That(ex.ParamName, Is.EqualTo("rate"));
        }

        [TestCase(0u, 0, 2.0, 0)]
        [TestCase(1u, 0, 2.0, 0)]
        [TestCase(0u, 1000, 2.0, 1000)]
        [TestCase(1u, 1000, 2.0, 2000)]
        [TestCase(3u, 1000, 2.0, 8000)]
        [TestCase(0u, 1000, 3.0, 1000)]
        [TestCase(1u, 1000, 3.0, 3000)]
        [TestCase(2u, 1000, 3.0, 9000)]
        public void TryGetRetryDelay_ReturnsExpectedDelay(uint attempts, int initialDelayMs, double rate, int expectedDelayMs)
        {
            var initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            var expectedDelay = TimeSpan.FromMilliseconds(expectedDelayMs);
            var strategy = new ExponentialStrategy(initialDelay, rate);

            var result = strategy.TryGetRetryDelay(TimeSpan.Zero, attempts, out var actualDelay);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualDelay, Is.EqualTo(expectedDelay));
            });
        }

        [TestCase(0u, 0, 2.0, 0)]
        [TestCase(1u, 0, 2.0, 0)]
        [TestCase(0u, 1000, 2.0, 1000)]
        [TestCase(1u, 1000, 2.0, 2000)]
        [TestCase(3u, 1000, 2.0, 8000)]
        [TestCase(0u, 1000, 3.0, 1000)]
        [TestCase(1u, 1000, 3.0, 3000)]
        [TestCase(2u, 1000, 3.0, 9000)]
        public void TryGetRetryDelay_IgnoresElapsed(uint attempts, int initialDelayMs, double rate, int expectedDelayMs)
        {
            var initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            var expectedDelay = TimeSpan.FromMilliseconds(expectedDelayMs);
            var strategy = new ExponentialStrategy(initialDelay, rate);

            var result = strategy.TryGetRetryDelay(TimeSpan.FromHours(1), attempts, out var actualDelay);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualDelay, Is.EqualTo(expectedDelay));
            });
        }
    }
}
