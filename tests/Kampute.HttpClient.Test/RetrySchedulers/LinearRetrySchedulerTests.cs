namespace Kampute.HttpClient.Test.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class LinearRetrySchedulerTests
    {
        [Test]
        public async Task Scheduler_AppliesCorrectDelay()
        {
            var maxAttempts = 3;

            var initialDelay = TimeSpan.FromMilliseconds(5);
            var delayStep = TimeSpan.FromMilliseconds(1);
            var scheduler = new LinearRetryScheduler(initialDelay, delayStep);

            var expectedDelay = initialDelay;
            for (var attempts = 0; attempts < maxAttempts; attempts++)
            {
                Assert.That(scheduler.Delay, Is.EqualTo(expectedDelay));
                await scheduler.WaitAsync(CancellationToken.None);
                expectedDelay += delayStep;
            }
        }

        [Test]
        public async Task Scheduler_UpdatesAttempts()
        {
            var maxAttempts = 3;

            var initialDelay = TimeSpan.FromMilliseconds(5);
            var delayStep = TimeSpan.FromMilliseconds(1);
            var scheduler = new LinearRetryScheduler(initialDelay, delayStep);

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
            var delayStep = TimeSpan.FromMilliseconds(1);
            var scheduler = new LinearRetryScheduler(initialDelay, delayStep);

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
            var delayStep = TimeSpan.FromMilliseconds(1);
            var scheduler = new LinearRetryScheduler(initialDelay, delayStep);
            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await scheduler.WaitAsync(cancellationTokenSource.Token));
        }
    }
}
