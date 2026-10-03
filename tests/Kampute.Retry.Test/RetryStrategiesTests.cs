namespace Kampute.Retry.Test
{
    using Kampute.Retry.Strategies;
    using NUnit.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class RetryStrategiesTests
    {
        private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

        [Test]
        public void None_NeverRetries()
        {
            Assert.That(RetryStrategies.None.TryGetRetryDelay(TimeSpan.Zero, 0, out _), Is.False);
        }

        [Test]
        public void Once_RetriesOnceAfterTheDelay()
        {
            var strategy = RetryStrategies.Once(Second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(strategy.TryGetRetryDelay(TimeSpan.Zero, 0, out var delay), Is.True);
                Assert.That(delay, Is.EqualTo(Second));
                Assert.That(strategy.TryGetRetryDelay(TimeSpan.Zero, 1, out _), Is.False);
            }
        }

        [Test]
        public void Once_WithTime_RetriesOnceAtThatTime()
        {
            var strategy = RetryStrategies.Once(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(strategy.TryGetRetryDelay(TimeSpan.Zero, 0, out var delay), Is.True);
                Assert.That(delay, Is.EqualTo(TimeSpan.FromMinutes(1)).Within(TimeSpan.FromSeconds(5)));
                Assert.That(strategy.TryGetRetryDelay(TimeSpan.Zero, 1, out _), Is.False);
            }
        }

        [Test]
        public void Factories_CreateTheBuiltInStrategies()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(RetryStrategies.Uniform(Second), Is.TypeOf<UniformStrategy>());
                Assert.That(RetryStrategies.Linear(Second), Is.TypeOf<LinearStrategy>());
                Assert.That(RetryStrategies.Linear(Second, Second), Is.TypeOf<LinearStrategy>());
                Assert.That(RetryStrategies.Fibonacci(Second), Is.TypeOf<FibonacciStrategy>());
                Assert.That(RetryStrategies.Fibonacci(Second, Second), Is.TypeOf<FibonacciStrategy>());
                Assert.That(RetryStrategies.Exponential(Second), Is.TypeOf<ExponentialStrategy>());
            }
        }

        [Test]
        public void Factories_CreateStrategiesWithoutLimits()
        {
            var strategies = new[]
            {
                RetryStrategies.Uniform(Second),
                RetryStrategies.Linear(Second),
                RetryStrategies.Fibonacci(Second),
                RetryStrategies.Exponential(Second, 1.0),
            };

            Assert.That(strategies, Has.All.Matches<IRetryStrategy>(strategy => strategy.TryGetRetryDelay(TimeSpan.FromDays(365), 1000, out _)));
        }

        [Test]
        public void GrowingDelays_SaturateInsteadOfOverflowing()
        {
            var strategies = new[]
            {
                RetryStrategies.Linear(TimeSpan.FromDays(1)),
                RetryStrategies.Fibonacci(Second),
                RetryStrategies.Exponential(Second),
                RetryStrategies.Exponential(Second).WithJitter(0.5),
            };

            foreach (var strategy in strategies)
            {
                Assert.That(strategy.TryGetRetryDelay(TimeSpan.Zero, uint.MaxValue, out var delay), Is.True, strategy.GetType().Name);
                Assert.That(delay, Is.GreaterThanOrEqualTo(TimeSpan.FromDays(1_000_000)), strategy.GetType().Name);
            }
        }

        [Test]
        public async Task Session_WithSaturatedDelay_StopsRetrying()
        {
            var session = RetryStrategies.Exponential(TimeSpan.FromMilliseconds(1), 1e12).StartSession();

            var results = new[]
            {
                await session.WaitAsync(CancellationToken.None),
                await session.WaitAsync(CancellationToken.None),
                await session.WaitAsync(CancellationToken.None),
            };

            Assert.That(results, Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void Exponential_DefaultsToRateTwo()
        {
            var strategy = (ExponentialStrategy)RetryStrategies.Exponential(Second);

            Assert.That(strategy.Rate, Is.EqualTo(2.0));
        }

        [Test]
        public void Modifiers_CombineInAnyOrder()
        {
            var strategy = RetryStrategies.Uniform(Second)
                .WithJitter(0.2)
                .WithMaxAttempts(2)
                .WithTimeout(TimeSpan.FromMinutes(1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(strategy.TryGetRetryDelay(TimeSpan.Zero, 1, out var delay), Is.True);
                Assert.That(delay, Is.EqualTo(Second).Within(TimeSpan.FromMilliseconds(200)));
                Assert.That(strategy.TryGetRetryDelay(TimeSpan.Zero, 2, out _), Is.False);
                Assert.That(strategy.TryGetRetryDelay(TimeSpan.FromMinutes(2), 0, out _), Is.False);
            }
        }
    }
}
