namespace Kampute.HttpClient.Test.RetryManagement.Strategies
{
    using Kampute.HttpClient.RetryManagement.Strategies;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class UniformStrategyTests
    {
        [Test]
        public void Constructor_SetsDelayProperty()
        {
            var expectedDelay = TimeSpan.FromSeconds(5);

            var strategy = new UniformStrategy(expectedDelay);

            Assert.That(strategy.Delay, Is.EqualTo(expectedDelay));
        }

        [TestCase(0u, 0)]
        [TestCase(5u, 10)]
        [TestCase(100u, 1000)]
        public void TryGetRetryDelay_ReturnsExpectedDelay(uint attempts, int delayMilliseconds)
        {
            var expectedDelay = TimeSpan.FromMilliseconds(delayMilliseconds);
            var strategy = new UniformStrategy(expectedDelay);

            var result = strategy.TryGetRetryDelay(TimeSpan.Zero, attempts, out var actualDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(actualDelay, Is.EqualTo(expectedDelay));
            }
        }

        [TestCase(0u)]
        [TestCase(5u)]
        [TestCase(100u)]
        public void TryGetRetryDelay_IgnoresElapsedAndAttempts(uint attempts)
        {
            var expectedDelay = TimeSpan.FromSeconds(10);
            var strategy = new UniformStrategy(expectedDelay);

            var result = strategy.TryGetRetryDelay(TimeSpan.FromHours(1), attempts, out var actualDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(actualDelay, Is.EqualTo(expectedDelay));
            }
        }
    }
}
