namespace Kampute.HttpClient.Test.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class AttemptLimitRetrySchedulerTests
    {
        [Test]
        public async Task Scheduler_UpdatesAttemptsUntilMaxAttempts()
        {
            var delay = TimeSpan.FromMilliseconds(10);
            var baseScheduler = new UniformRetryScheduler(delay);

            var maxAttempts = 3;
            var scheduler = new AttemptLimitRetryScheduler(baseScheduler, maxAttempts);

            var attempts = 0;
            while (await scheduler.WaitAsync(CancellationToken.None))
                attempts++;

            Assert.That(scheduler.Attempts, Is.EqualTo(attempts));
            Assert.That(scheduler.Attempts, Is.EqualTo(maxAttempts));
        }

        [Test]
        public async Task Scheduler_ResetsProperly()
        {
            var delay = TimeSpan.FromMilliseconds(10);
            var baseScheduler = new UniformRetryScheduler(delay);

            var maxAttempts = 3;
            var scheduler = new AttemptLimitRetryScheduler(baseScheduler, maxAttempts);

            while (await scheduler.WaitAsync(CancellationToken.None)) { }
            scheduler.Reset();

            var shouldRetry = await scheduler.WaitAsync(CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(shouldRetry, Is.True);
                Assert.That(scheduler.Attempts, Is.EqualTo(1));
            });
        }

        [Test]
        public void Scheduler_OnCancellation_ThrowsTaskCanceledException()
        {
            var delay = TimeSpan.FromMilliseconds(100);
            var baseScheduler = new UniformRetryScheduler(delay);

            var maxAttempts = 3;
            var scheduler = new AttemptLimitRetryScheduler(baseScheduler, maxAttempts);
            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await scheduler.WaitAsync(cancellationTokenSource.Token));
        }
    }
}
