namespace Kampute.HttpClient.NetFramework.Test
{
    using Kampute.Retry;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class RetrySessionTests
    {
        [Test]
        public async Task WaitAsync_WhenDelayExceedsLimit_ReturnsFalseWithoutRetrying()
        {
            var session = new RetrySession(new FixedDelayStrategy(TimeSpan.FromDays(30)));

            var result = await session.WaitAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(session.Attempts, Is.Zero);
            }
        }

        [Test]
        public void Wait_WhenDelayExceedsLimit_ReturnsFalseWithoutRetrying()
        {
            var session = new RetrySession(new FixedDelayStrategy(TimeSpan.FromDays(30)));

            var result = session.Wait(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(session.Attempts, Is.Zero);
            }
        }

        [Test]
        public void Wait_WhenCanceledDuringTheDelay_ThrowsPromptly()
        {
            var session = new RetrySession(new FixedDelayStrategy(TimeSpan.FromMinutes(1)));
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            var timer = System.Diagnostics.Stopwatch.StartNew();
            Assert.Throws<OperationCanceledException>(() => session.Wait(cancellationTokenSource.Token));
            timer.Stop();

            Assert.That(timer.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
        }

        private sealed class FixedDelayStrategy : IRetryStrategy
        {
            private readonly TimeSpan _delay;

            public FixedDelayStrategy(TimeSpan delay) => _delay = delay;

            public bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay)
            {
                delay = _delay;
                return true;
            }
        }
    }
}
