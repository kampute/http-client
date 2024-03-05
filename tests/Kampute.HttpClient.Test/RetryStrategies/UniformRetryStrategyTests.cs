namespace Kampute.HttpClient.Test.RetryStrategies
{
    using Kampute.HttpClient.RetrySchedulers;
    using Kampute.HttpClient.RetryStrategies;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class UniformRetryStrategyTests
    {
        [Test]
        public void Strategy_WithAttemptLimit_CreatesSchedulerWithCorrectProperties()
        {
            var maxAttempts = 5;
            var delay = TimeSpan.FromSeconds(100);
            var strategy = new UniformRetryStrategy(maxAttempts, delay);

            var scheduler = strategy.CreateScheduler() as AttemptLimitRetryScheduler;
            Assert.That(scheduler, Is.Not.Null);
            Assert.That(scheduler.MaxAttempts, Is.EqualTo(maxAttempts));

            var baseScheduler = scheduler.BaseScheduler as UniformRetryScheduler;
            Assert.That(baseScheduler, Is.Not.Null);
            Assert.That(baseScheduler.Delay, Is.EqualTo(delay));
        }

        [Test]
        public void Strategy_WithTimeLimit_CreatesSchedulerWithCorrectProperties()
        {
            var timeout = TimeSpan.FromMinutes(10);
            var delay = TimeSpan.FromSeconds(100);
            var strategy = new UniformRetryStrategy(timeout, delay);

            var scheduler = strategy.CreateScheduler() as TimeLimitRetryScheduler;
            Assert.That(scheduler, Is.Not.Null);
            Assert.That(scheduler.Timeout, Is.EqualTo(timeout));

            var baseScheduler = scheduler.BaseScheduler as UniformRetryScheduler;
            Assert.That(baseScheduler, Is.Not.Null);
            Assert.That(baseScheduler.Delay, Is.EqualTo(delay));
        }
    }
}
