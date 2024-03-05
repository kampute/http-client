namespace Kampute.HttpClient.Test.RetryStrategies
{
    using Kampute.HttpClient.RetrySchedulers;
    using Kampute.HttpClient.RetryStrategies;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class FibonacciRetryStrategyTests
    {
        [Test]
        public void Strategy_WithAttemptLimit_CreatesSchedulerWithCorrectProperties()
        {
            var maxAttempts = 5;
            var initialDelay = TimeSpan.FromSeconds(100);
            var strategy = new FibonacciRetryStrategy(maxAttempts, initialDelay);

            var scheduler = strategy.CreateScheduler() as AttemptLimitRetryScheduler;
            Assert.That(scheduler, Is.Not.Null);
            Assert.That(scheduler.MaxAttempts, Is.EqualTo(maxAttempts));

            var baseScheduler = scheduler.BaseScheduler as FibonacciRetryScheduler;
            Assert.That(baseScheduler, Is.Not.Null);
            Assert.That(baseScheduler.InitialDelay, Is.EqualTo(initialDelay));
        }

        [Test]
        public void Strategy_WithTimeLimit_CreatesSchedulerWithCorrectProperties()
        {
            var timeout = TimeSpan.FromMinutes(10);
            var initialDelay = TimeSpan.FromSeconds(100);
            var strategy = new FibonacciRetryStrategy(timeout, initialDelay);

            var scheduler = strategy.CreateScheduler() as TimeLimitRetryScheduler;
            Assert.That(scheduler, Is.Not.Null);
            Assert.That(scheduler.Timeout, Is.EqualTo(timeout));

            var baseScheduler = scheduler.BaseScheduler as FibonacciRetryScheduler;
            Assert.That(baseScheduler, Is.Not.Null);
            Assert.That(baseScheduler.InitialDelay, Is.EqualTo(initialDelay));
        }
    }
}
