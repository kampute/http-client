namespace Kampute.HttpClient.NetFramework.Test
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class RetrySchedulerTests
    {
        [Test]
        public async Task WaitAsync_WhenDelayExceedsLimit_ReturnsFalseWithoutRetrying()
        {
            var scheduler = new RetryScheduler(new FixedDelayStrategy(TimeSpan.FromDays(30)));

            var result = await scheduler.WaitAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(scheduler.Attempts, Is.Zero);
            }
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
