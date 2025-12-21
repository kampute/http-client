namespace Kampute.HttpClient.Test.RetryManagement.Strategies.Modifiers
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement.Strategies.Modifiers;
    using Moq;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class JitterStrategyModifierTests
    {
        [TestCase(-0.1)]
        [TestCase(1.1)]
        public void Constructor_WhenJitterFactorIsOutOfRange_ThrowsArgumentOutOfRangeException(double jitterFactor)
        {
            var mockSource = new Mock<IRetryStrategy>();

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new JitterStrategyModifier(mockSource.Object, jitterFactor));

            Assert.That(ex.ParamName, Is.EqualTo("jitterFactor"));
        }

        [TestCase(0.0, 1000)]
        [TestCase(0.5, 1000)]
        [TestCase(1.0, 1000)]
        public void TryGetRetryDelay_WhenSourceReturnsTrue_ReturnsTrueWithJitteredDelay(double jitterFactor, int baseDelayMs)
        {
            var baseDelay = TimeSpan.FromMilliseconds(baseDelayMs);
            var mockSource = new Mock<IRetryStrategy>();
            mockSource.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out baseDelay)).Returns(true);
            var strategy = new JitterStrategyModifier(mockSource.Object, jitterFactor);

            var result = strategy.TryGetRetryDelay(TimeSpan.Zero, 0, out var actualDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(actualDelay, Is.EqualTo(baseDelay).Within(jitterFactor * baseDelay));
            }
        }

        [Test]
        public void TryGetRetryDelay_WhenSourceReturnsFalse_ReturnsFalse()
        {
            var mockSource = new Mock<IRetryStrategy>();
            mockSource.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(false);
            var strategy = new JitterStrategyModifier(mockSource.Object, 0.5);

            var result = strategy.TryGetRetryDelay(TimeSpan.Zero, 0, out var actualDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(actualDelay, Is.Default);
            }
        }
    }
}
