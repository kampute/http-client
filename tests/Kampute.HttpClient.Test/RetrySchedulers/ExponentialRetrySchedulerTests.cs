namespace Kampute.HttpClient.Test.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class ExponentialRetrySchedulerTests
    {
        [Test]
        public async Task Scheduler_AppliesCorrectDelay()
        {
            var maxAttempts = 3;

            var initialDelay = TimeSpan.FromMilliseconds(5);
            var rate = 3.0;
            var scheduler = new ExponentialRetryScheduler(initialDelay, rate);

            var fibCurrent = 0;
            var fibNext = 1;
            for (var attempts = 0; attempts < maxAttempts; attempts++)
            {
                var expectedDelay = initialDelay * Math.Pow(rate, attempts);
                Assert.That(scheduler.Delay, Is.EqualTo(expectedDelay));
                await scheduler.WaitAsync(CancellationToken.None);
                (fibCurrent, fibNext) = (fibNext, fibCurrent + fibNext);
            }
        }

        [Test]
        public async Task Scheduler_UpdatesAttempts()
        {
            var maxAttempts = 3;

            var initialDelay = TimeSpan.FromMilliseconds(5);
            var rate = 3.0;
            var scheduler = new ExponentialRetryScheduler(initialDelay, rate);

            for (var attempts = 0; attempts < maxAttempts; attempts++)
            {
                Assert.That(scheduler.Attempts, Is.EqualTo(attempts));
                await scheduler.WaitAsync(CancellationToken.None);
            }

            Assert.That(scheduler.Attempts, Is.EqualTo(maxAttempts));
        }

        [Test]
        public async Task Scheduler_ResetsProperly()
        {
            var initialDelay = TimeSpan.FromMilliseconds(5);
            var rate = 3.0;
            var scheduler = new ExponentialRetryScheduler(initialDelay, rate);

            await scheduler.WaitAsync(CancellationToken.None);
            await scheduler.WaitAsync(CancellationToken.None);
            scheduler.Reset();

            await scheduler.WaitAsync(CancellationToken.None);

            Assert.That(scheduler.Attempts, Is.EqualTo(1));
        }

        [Test]
        public void Scheduler_OnCancellation_ThrowsTaskCanceledException()
        {
            var initialDelay = TimeSpan.FromMilliseconds(5);
            var rate = 3.0;
            var scheduler = new ExponentialRetryScheduler(initialDelay, rate);
            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await scheduler.WaitAsync(cancellationTokenSource.Token));
        }
    }
}
