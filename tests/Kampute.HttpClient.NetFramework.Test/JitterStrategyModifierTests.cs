namespace Kampute.HttpClient.NetFramework.Test
{
    using Kampute.Retry;
    using Kampute.Retry.Strategies.Modifiers;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [TestFixture]
    public class JitterStrategyModifierTests
    {
        [Test]
        public void TryGetRetryDelay_WhenCalledConcurrently_KeepsProducingRandomDelays()
        {
            var strategy = new JitterStrategyModifier(new FixedDelayStrategy(TimeSpan.FromSeconds(1)), 1.0);

            Parallel.For(0, 1_000_000, _ => strategy.TryGetRetryDelay(TimeSpan.Zero, 0, out var _));

            var delays = Enumerable.Range(0, 100).Select(_ =>
            {
                strategy.TryGetRetryDelay(TimeSpan.Zero, 0, out var delay);
                return delay;
            });

            Assert.That(delays.Distinct().Count(), Is.GreaterThan(1));
        }

        private sealed class FixedDelayStrategy : IRetryStrategy
        {
            private readonly TimeSpan _delay;

            public FixedDelayStrategy(TimeSpan delay) => _delay = delay;

            public bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay)
            {
                delay = _delay;
                return true;
            }
        }
    }
}
