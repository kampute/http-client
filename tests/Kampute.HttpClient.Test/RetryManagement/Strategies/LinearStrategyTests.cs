namespace Kampute.HttpClient.Test.RetryManagement.Strategies
{
    using Kampute.HttpClient.RetryManagement.Strategies;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class LinearStrategyTests
    {
        [TestCase(0, 0)]
        [TestCase(100, 10)]
        [TestCase(500, 50)]
        public void Constructor_WithInitialDelayAndDelayStep_SetsPropertiesCorrectly(int initialDelayMs, int delayStepMs)
        {
            var initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            var delayStep = TimeSpan.FromMilliseconds(delayStepMs);

            var strategy = new LinearStrategy(initialDelay, delayStep);

            Assert.Multiple(() =>
            {
                Assert.That(strategy.InitialDelay, Is.EqualTo(initialDelay));
                Assert.That(strategy.DelayStep, Is.EqualTo(delayStep));
            });
        }

        [TestCase(0)]
        [TestCase(100)]
        [TestCase(500)]
        public void Constructor_WithInitialDelayOnly_SetsInitialDelayAndDelayStep(int initialDelayMs)
        {
            var initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);

            var strategy = new LinearStrategy(initialDelay);

            Assert.Multiple(() =>
            {
                Assert.That(strategy.InitialDelay, Is.EqualTo(initialDelay));
                Assert.That(strategy.DelayStep, Is.EqualTo(initialDelay));
            });
        }

        [TestCase(0u, 0, 0, 0)]
        [TestCase(0u, 0, 100, 0)]
        [TestCase(0u, 1000, 100, 1000)]
        [TestCase(1u, 0, 0, 0)]
        [TestCase(1u, 0, 100, 100)]
        [TestCase(1u, 1000, 100, 1100)]
        [TestCase(3u, 1000, 100, 1300)]
        [TestCase(6u, 1000, 100, 1600)]
        public void TryGetRetryDelay_ReturnsExpectedDelay(uint attempts, int initialDelayMs, int delayStepMs, int expectedDelayMs)
        {
            var initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            var delayStep = TimeSpan.FromMilliseconds(delayStepMs);
            var expectedDelay = TimeSpan.FromMilliseconds(expectedDelayMs);
            var strategy = new LinearStrategy(initialDelay, delayStep);

            var result = strategy.TryGetRetryDelay(TimeSpan.Zero, attempts, out var actualDelay);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualDelay, Is.EqualTo(expectedDelay));
            });
        }

        [TestCase(0u, 0, 0, 0)]
        [TestCase(0u, 0, 100, 0)]
        [TestCase(0u, 1000, 100, 1000)]
        [TestCase(1u, 0, 0, 0)]
        [TestCase(1u, 0, 100, 100)]
        [TestCase(1u, 1000, 100, 1100)]
        [TestCase(3u, 1000, 100, 1300)]
        [TestCase(6u, 1000, 100, 1600)]
        public void TryGetRetryDelay_IgnoresElapsed(uint attempts, int initialDelayMs, int delayStepMs, int expectedDelayMs)
        {
            var initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            var delayStep = TimeSpan.FromMilliseconds(delayStepMs);
            var expectedDelay = TimeSpan.FromMilliseconds(expectedDelayMs);
            var strategy = new LinearStrategy(initialDelay, delayStep);

            var result = strategy.TryGetRetryDelay(TimeSpan.FromHours(1), attempts, out var actualDelay);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(actualDelay, Is.EqualTo(expectedDelay));
            });
        }
    }
}
