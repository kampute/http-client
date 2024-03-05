namespace Kampute.HttpClient.Test.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class FibonacciRetrySchedulerTests
    {
        [Test]
        public async Task Scheduler_AppliesCorrectDelay()
        {
            var maxAttempts = 3;

            var initialDelay = TimeSpan.FromMilliseconds(5);
            var scheduler = new FibonacciRetryScheduler(initialDelay);

            var fibPrevious = 1;
            var fibCurrent = 1;
            for (var attempts = 0; attempts < maxAttempts; attempts++)
            {
                var expectedDelay = initialDelay * fibCurrent;
                Assert.That(scheduler.Delay, Is.EqualTo(expectedDelay));
                await scheduler.WaitAsync(CancellationToken.None);
                (fibPrevious, fibCurrent) = (fibCurrent, fibPrevious + fibCurrent);
            }
        }

        [Test]
        public async Task Scheduler_UpdatesAttempts()
        {
            var maxAttempts = 3;

            var initialDelay = TimeSpan.FromMilliseconds(5);
            var scheduler = new FibonacciRetryScheduler(initialDelay);

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
            var scheduler = new FibonacciRetryScheduler(initialDelay);

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
            var scheduler = new FibonacciRetryScheduler(initialDelay);

            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await scheduler.WaitAsync(cancellationTokenSource.Token));
        }
    }
}
