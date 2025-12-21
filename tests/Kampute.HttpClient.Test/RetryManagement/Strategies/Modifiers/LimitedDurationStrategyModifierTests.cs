namespace Kampute.HttpClient.Test.RetryManagement.Strategies.Modifiers
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement.Strategies.Modifiers;
    using Moq;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class LimitedDurationStrategyModifierTests
    {
        [TestCase(1000, 0, 100, 100, true)]
        [TestCase(1000, 500, 100, 100, true)]
        [TestCase(1000, 950, 100, 50, true)]
        [TestCase(1000, 1000, 100, 0, false)]
        [TestCase(1000, 1500, 100, 0, false)]
        public void TryGetRetryDelay_WhenSourceReturnsTrue_ReturnsExpectedResult(int timeoutMs, int elapsedMs, int baseDelayMs, int expectedDelayMs, bool expectedResult)
        {
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            var elapsed = TimeSpan.FromMilliseconds(elapsedMs);
            var baseDelay = TimeSpan.FromMilliseconds(baseDelayMs);
            var expectedDelay = TimeSpan.FromMilliseconds(expectedDelayMs);

            var mockSource = new Mock<IRetryStrategy>();
            mockSource.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny))
                      .Returns((TimeSpan elapsed, uint attempts, out TimeSpan delay) =>
                      {
                          delay = expectedDelay;
                          return true;
                      });
            var strategy = new LimitedDurationStrategyModifier(mockSource.Object, timeout);

            var result = strategy.TryGetRetryDelay(elapsed, 0, out var actualDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(expectedResult));
                Assert.That(actualDelay, Is.EqualTo(expectedDelay));
            }
        }

        [Test]
        public void TryGetRetryDelay_WhenSourceReturnsFalse_ReturnsFalse()
        {
            var mockSource = new Mock<IRetryStrategy>();
            mockSource.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(false);
            var strategy = new LimitedDurationStrategyModifier(mockSource.Object, TimeSpan.FromSeconds(10));

            var result = strategy.TryGetRetryDelay(TimeSpan.Zero, 0, out var actualDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(actualDelay, Is.Default);
            }
        }
    }
}
