namespace Kampute.HttpClient.Test.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class TimeLimitRetrySchedulerTests
    {
        [Test]
        public async Task Scheduler_StopsRetryingAfterTimeout()
        {
            var delay = TimeSpan.FromMilliseconds(30);
            var baseScheduler = new UniformRetryScheduler(delay);

            var timeout = TimeSpan.FromMilliseconds(100);
            var scheduler = new TimeLimitRetryScheduler(baseScheduler, timeout);

            var startTime = Stopwatch.StartNew();
            while (await scheduler.WaitAsync(CancellationToken.None)) { }
            var elapsedTime = startTime.Elapsed;

            Assert.That(elapsedTime, Is.GreaterThanOrEqualTo(timeout));
            Assert.That(elapsedTime, Is.LessThan(timeout + delay));
        }

        [Test]
        public async Task Scheduler_UpdatesAttemptsUntilTimeout()
        {
            var delay = TimeSpan.FromMilliseconds(30);
            var baseScheduler = new UniformRetryScheduler(delay);

            var timeout = TimeSpan.FromMilliseconds(100);
            var scheduler = new TimeLimitRetryScheduler(baseScheduler, timeout);

            var attempts = 0;
            while (await scheduler.WaitAsync(CancellationToken.None))
                attempts++;

            var expectedAttempts = (int)Math.Max(1 + (timeout - delay).TotalMilliseconds / delay.TotalMilliseconds, 0);
            Assert.That(scheduler.Attempts, Is.EqualTo(expectedAttempts));
        }

        [Test]
        public async Task Scheduler_ResetsProperly()
        {
            var delay = TimeSpan.FromMilliseconds(10);
            var baseScheduler = new UniformRetryScheduler(delay);

            var timeout = TimeSpan.FromMilliseconds(50);
            var scheduler = new TimeLimitRetryScheduler(baseScheduler, timeout);

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

            var timeout = TimeSpan.FromSeconds(50);
            var scheduler = new TimeLimitRetryScheduler(baseScheduler, timeout);
            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await scheduler.WaitAsync(cancellationTokenSource.Token));
        }
    }
}
