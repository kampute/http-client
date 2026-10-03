namespace Kampute.Retry.Test
{
    using Kampute.Retry;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class RetrySessionTests
    {
        [Test]
        public void Constructor_SetsStrategy_ToProvidedStrategy()
        {
            var mockStrategy = new Mock<IRetryStrategy>();

            var session = new RetrySession(mockStrategy.Object);

            Assert.That(session.Strategy, Is.SameAs(mockStrategy.Object));
        }

        [Test]
        public void Attempts_InitiallyReturnsZero()
        {
            var mockStrategy = new Mock<IRetryStrategy>();
            var session = new RetrySession(mockStrategy.Object);

            Assert.That(session.Attempts, Is.Zero);
        }

        [Test]
        public async Task WaitAsync_WaitsAccordingToStrategy()
        {
            var expectedDelay = TimeSpan.FromMilliseconds(50);

            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out expectedDelay)).Returns(true);
            var session = new RetrySession(mockStrategy.Object);

            var timer = Stopwatch.StartNew();
            var result = await session.WaitAsync(CancellationToken.None);
            timer.Stop();

            Assert.That(timer.Elapsed, Is.EqualTo(expectedDelay).Within(TimeSpan.FromMilliseconds(100)));
        }

        [Test]
        public async Task WaitAsync_WhenStrategyIndicatesRetryIsAdvisable_ReturnsTrue()
        {
            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(true);
            var session = new RetrySession(mockStrategy.Object);

            var result = await session.WaitAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(session.Attempts, Is.EqualTo(1u));
            }
        }

        [Test]
        public async Task WaitAsync_WhenStrategyIndicatesRetryIsNotAdvisable_ReturnsFalse()
        {
            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(false);
            var session = new RetrySession(mockStrategy.Object);

            var result = await session.WaitAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(session.Attempts, Is.Zero);
            }
        }

        [Test]
        public void WaitAsync_WhenCanceled_ThrowsOperationCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(true);
            var session = new RetrySession(mockStrategy.Object);

            Assert.ThrowsAsync<OperationCanceledException>(() => session.WaitAsync(cancellationTokenSource.Token));
        }

        [Test]
        public async Task WaitAsync_WhenDelayExceedsLimit_ReturnsFalseWithoutRetrying()
        {
            var delay = TimeSpan.FromDays(60);

            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out delay)).Returns(true);
            var session = new RetrySession(mockStrategy.Object);

            var result = await session.WaitAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(session.Attempts, Is.Zero);
            }
        }

        [Test]
        public void Wait_BlocksAccordingToStrategyAndCountsTheAttempt()
        {
            var expectedDelay = TimeSpan.FromMilliseconds(50);
            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out expectedDelay)).Returns(true);
            var session = new RetrySession(mockStrategy.Object);

            var timer = Stopwatch.StartNew();
            var result = session.Wait(CancellationToken.None);
            timer.Stop();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(session.Attempts, Is.EqualTo(1));
                Assert.That(timer.Elapsed, Is.EqualTo(expectedDelay).Within(TimeSpan.FromMilliseconds(100)));
            }
        }

        [Test]
        public void Wait_WhenStrategyStopsOrDelayExceedsLimit_ReturnsFalseWithoutWaiting()
        {
            var tooLong = TimeSpan.FromDays(60);
            var exceeding = new Mock<IRetryStrategy>();
            exceeding.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out tooLong)).Returns(true);
            var stopping = new Mock<IRetryStrategy>();
            stopping.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(new RetrySession(exceeding.Object).Wait(CancellationToken.None), Is.False);
                Assert.That(new RetrySession(stopping.Object).Wait(CancellationToken.None), Is.False);
            }
        }

        [Test]
        public void Wait_WhenCanceledDuringTheDelay_ThrowsPromptly()
        {
            var longDelay = TimeSpan.FromMinutes(1);
            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out longDelay)).Returns(true);
            var session = new RetrySession(mockStrategy.Object);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            var timer = Stopwatch.StartNew();
            Assert.Throws<OperationCanceledException>(() => session.Wait(cancellationTokenSource.Token));
            timer.Stop();

            Assert.That(timer.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public async Task Reset_ResetsInternalState()
        {
            var mockStrategy = new Mock<IRetryStrategy>();
            mockStrategy.Setup(s => s.TryGetRetryDelay(It.IsAny<TimeSpan>(), It.IsAny<uint>(), out It.Ref<TimeSpan>.IsAny)).Returns(true);
            var session = new RetrySession(mockStrategy.Object);

            await session.WaitAsync(CancellationToken.None);
            session.Reset();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(session.Elapsed, Is.LessThanOrEqualTo(TimeSpan.FromMilliseconds(10)));
                Assert.That(session.Attempts, Is.Zero);
            }
        }
    }
}
