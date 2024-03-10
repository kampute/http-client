namespace Kampute.HttpClient.Test.RetryManagement
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class RetrySchedulerTests
    {
        [Test]
        public void Constructor_SetsStrategy_ToProvidedStrategy()
        {
            var mockStrategy = new Mock<IRetryStrategy>();

            var scheduler = new RetryScheduler(mockStrategy.Object);

            Assert.That(scheduler.Strategy, Is.SameAs(mockStrategy.Object));
        }

        [Test]
        public void Attempts_InitiallyReturnsZero()
        {
            var mockStrategy = new Mock<IRetryStrategy>();
            var scheduler = new RetryScheduler(mockStrategy.Object);

            Assert.That(scheduler.Attempts, Is.Zero);
        }

        [Test]
        public async Task WaitAsync_WaitsAccordingToStrategy()
        {
            var expectedDelay = TimeSpan.FromMilliseconds(100);

            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out expectedDelay)).Returns(true);
            var scheduler = new RetryScheduler(mockStrategy.Object);

            var timer = Stopwatch.StartNew();
            var result = await scheduler.WaitAsync(CancellationToken.None);
            timer.Stop();

            Assert.That(timer.Elapsed, Is.InRange(expectedDelay, 1.2 * expectedDelay));
        }

        [Test]
        public async Task WaitAsync_WhenStrategyIndicatesRetryIsAdvisable_ReturnsTrue()
        {
            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(true);
            var scheduler = new RetryScheduler(mockStrategy.Object);

            var result = await scheduler.WaitAsync(CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(scheduler.Attempts, Is.EqualTo(1u));
            });
        }

        [Test]
        public async Task WaitAsync_WhenStrategyIndicatesRetryIsNotAdvisable_ReturnsFalse()
        {
            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(false);
            var scheduler = new RetryScheduler(mockStrategy.Object);

            var result = await scheduler.WaitAsync(CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(scheduler.Attempts, Is.Zero);
            });
        }

        [Test]
        public void WaitAsync_WhenCanceled_ThrowsTaskCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(true);
            var scheduler = new RetryScheduler(mockStrategy.Object);

            Assert.ThrowsAsync<TaskCanceledException>(() => scheduler.WaitAsync(cancellationTokenSource.Token));
        }

        [Test]
        public async Task Reset_ResetsInternalState()
        {
            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(true);
            var scheduler = new RetryScheduler(mockStrategy.Object);

            await scheduler.WaitAsync(CancellationToken.None);
            scheduler.Reset();

            Assert.Multiple(() =>
            {
                Assert.That(scheduler.Elapsed, Is.LessThanOrEqualTo(TimeSpan.FromMilliseconds(10)));
                Assert.That(scheduler.Attempts, Is.Zero);
            });
        }
    }
}
