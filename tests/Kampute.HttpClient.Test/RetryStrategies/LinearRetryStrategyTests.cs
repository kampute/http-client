namespace Kampute.HttpClient.Test.RetryStrategies
{
    using Kampute.HttpClient.RetrySchedulers;
    using Kampute.HttpClient.RetryStrategies;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class LinearRetryStrategyTests
    {
        [Test]
        public void Strategy_WithAttemptLimit_CreatesSchedulerWithCorrectProperties()
        {
            var maxAttempts = 5;
            var initialDelay = TimeSpan.FromSeconds(100);
            var delayStep = TimeSpan.FromSeconds(10);
            var strategy = new LinearRetryStrategy(maxAttempts, initialDelay, delayStep);

            var scheduler = strategy.CreateScheduler() as AttemptLimitRetryScheduler;
            Assert.That(scheduler, Is.Not.Null);
            Assert.That(scheduler.MaxAttempts, Is.EqualTo(maxAttempts));

            var baseScheduler = scheduler.BaseScheduler as LinearRetryScheduler;
            Assert.That(baseScheduler, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(baseScheduler.InitialDelay, Is.EqualTo(initialDelay));
                Assert.That(baseScheduler.DelayStep, Is.EqualTo(delayStep));
            });
        }

        [Test]
        public void Strategy_WithTimeLimit_CreatesSchedulerWithCorrectProperties()
        {
            var timeout = TimeSpan.FromMinutes(10);
            var initialDelay = TimeSpan.FromSeconds(100);
            var delayStep = TimeSpan.FromSeconds(10);
            var strategy = new LinearRetryStrategy(timeout, initialDelay, delayStep);

            var scheduler = strategy.CreateScheduler() as TimeLimitRetryScheduler;
            Assert.That(scheduler, Is.Not.Null);
            Assert.That(scheduler.Timeout, Is.EqualTo(timeout));

            var baseScheduler = scheduler.BaseScheduler as LinearRetryScheduler;
            Assert.That(baseScheduler, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(baseScheduler.InitialDelay, Is.EqualTo(initialDelay));
                Assert.That(baseScheduler.DelayStep, Is.EqualTo(delayStep));
            });
        }
    }
}
