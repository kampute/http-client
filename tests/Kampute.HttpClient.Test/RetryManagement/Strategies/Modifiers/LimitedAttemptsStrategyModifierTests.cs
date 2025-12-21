namespace Kampute.HttpClient.Test.RetryManagement.Strategies.Modifiers
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement.Strategies.Modifiers;
    using Moq;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class LimitedAttemptsStrategyModifierTests
    {
        [TestCase(0u, 0u, false)]
        [TestCase(1u, 0u, true)]
        [TestCase(1u, 1u, false)]
        [TestCase(2u, 1u, true)]
        [TestCase(2u, 3u, false)]
        public void TryGetRetryDelay_WhenSourceReturnsTrue_ReturnsExpectedResult(uint maxAttempts, uint attempts, bool expectedResult)
        {
            var mockSource = new Mock<IRetryStrategy>();
            mockSource.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny))
                      .Returns((TimeSpan elapsed, uint attempts, out TimeSpan delay) =>
                      {
                          delay = TimeSpan.FromSeconds(1);
                          return true;
                      });
            var strategy = new LimitedAttemptsStrategyModifier(mockSource.Object, maxAttempts);

            var result = strategy.TryGetRetryDelay(TimeSpan.Zero, attempts, out var actualDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(expectedResult));
                Assert.That(actualDelay, expectedResult ? Is.Not.Default : Is.Default);
            }
        }

        [Test]
        public void TryGetRetryDelay_WhenSourceReturnsFalse_ReturnsFalse()
        {
            var mockSource = new Mock<IRetryStrategy>();
            mockSource.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(false);

            var strategy = new LimitedAttemptsStrategyModifier(mockSource.Object, 3);

            var result = strategy.TryGetRetryDelay(TimeSpan.Zero, 0, out var actualDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(actualDelay, Is.Default);
            }
        }
    }
}
