namespace Kampute.HttpClient.Test.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class JitterRetrySchedulerTests
    {
        [Test]
        public async Task Scheduler_AppliesCorrectDelay()
        {
            var maxAttempts = 3;

            var delay = TimeSpan.FromMilliseconds(5);
            var baseScheduler = new UniformRetryScheduler(delay);

            var jitterFactor = 0.1;
            var scheduler = new JitterRetryScheduler(baseScheduler, jitterFactor);

            for (var attempts = 0; attempts < maxAttempts; attempts++)
            {
                Assert.That(scheduler.Delay, Is.EqualTo(delay).Within(jitterFactor * delay));
                await scheduler.WaitAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task Scheduler_UpdatesAttempts()
        {
            var maxAttempts = 2;

            var delay = TimeSpan.FromMilliseconds(5);
            var baseScheduler = new UniformRetryScheduler(delay);

            var jitterFactor = 0.5;
            var scheduler = new JitterRetryScheduler(baseScheduler, jitterFactor);

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
            var delay = TimeSpan.FromMilliseconds(5);
            var baseScheduler = new UniformRetryScheduler(delay);

            var jitterFactor = 0.5;
            var scheduler = new JitterRetryScheduler(baseScheduler, jitterFactor);

            await scheduler.WaitAsync(CancellationToken.None);
            await scheduler.WaitAsync(CancellationToken.None);
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
            var delay = TimeSpan.FromMilliseconds(5);
            var baseScheduler = new UniformRetryScheduler(delay);

            var jitterFactor = 0.5;
            var scheduler = new JitterRetryScheduler(baseScheduler, jitterFactor);
            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await scheduler.WaitAsync(cancellationTokenSource.Token));
        }
    }
}
