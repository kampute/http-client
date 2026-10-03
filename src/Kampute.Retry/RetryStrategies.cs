// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.Retry package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.Retry
{
    using Kampute.Retry.Strategies;
    using System;

    /// <summary>
    /// Provides factory methods for the built-in retry strategies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Except for <see cref="None"/> and the <c>Once</c> methods, the strategies these methods create retry without limit. Chain
    /// <see cref="RetryStrategyExtensions.WithMaxAttempts"/>, <see cref="RetryStrategyExtensions.WithTimeout"/> and
    /// <see cref="RetryStrategyExtensions.WithJitter"/> to limit them and to spread their delays, in any combination:
    /// </para>
    /// <code>
    /// var retry = RetryStrategies.Exponential(TimeSpan.FromSeconds(1))
    ///     .WithJitter(0.2)
    ///     .WithMaxAttempts(5)
    ///     .WithTimeout(TimeSpan.FromMinutes(2));
    /// </code>
    /// <para>
    /// The strategies are listed here by how fast their delays grow: <c>Uniform</c> keeps the same delay, <c>Linear</c> adds a fixed step, <c>Fibonacci</c>
    /// follows the Fibonacci sequence, and <c>Exponential</c> multiplies the delay by a fixed rate.
    /// </para>
    /// </remarks>
    public static class RetryStrategies
    {
        /// <summary>
        /// Gets a strategy that never retries.
        /// </summary>
        /// <value>
        /// A strategy that never retries.
        /// </value>
        public static IRetryStrategy None => NoneStrategy.Instance;

        /// <summary>
        /// Creates a strategy that retries once, after the specified delay.
        /// </summary>
        /// <param name="delay">The delay before the retry.</param>
        /// <returns>A strategy that allows a single retry.</returns>
        public static IRetryStrategy Once(TimeSpan delay)
        {
            return new UniformStrategy(delay).WithMaxAttempts(1);
        }

        /// <summary>
        /// Creates a strategy that retries once, at the specified time.
        /// </summary>
        /// <param name="after">The time of the retry. The delay is computed from it when this method is called.</param>
        /// <returns>A strategy that allows a single retry.</returns>
        public static IRetryStrategy Once(DateTimeOffset after)
        {
            return new UniformStrategy(after - DateTimeOffset.UtcNow).WithMaxAttempts(1);
        }

        /// <summary>
        /// Creates a strategy that waits the same delay before every retry.
        /// </summary>
        /// <param name="delay">The delay before each retry.</param>
        /// <returns>A strategy that retries without limit.</returns>
        public static IRetryStrategy Uniform(TimeSpan delay)
        {
            return new UniformStrategy(delay);
        }

        /// <summary>
        /// Creates a strategy whose delay grows by the initial delay before each further retry.
        /// </summary>
        /// <param name="initialDelay">The delay before the first retry, which is also the amount added for each further retry.</param>
        /// <returns>A strategy that retries without limit.</returns>
        public static IRetryStrategy Linear(TimeSpan initialDelay)
        {
            return new LinearStrategy(initialDelay);
        }

        /// <summary>
        /// Creates a strategy whose delay grows by a fixed step before each further retry.
        /// </summary>
        /// <param name="initialDelay">The delay before the first retry.</param>
        /// <param name="delayStep">The amount added to the delay for each further retry.</param>
        /// <returns>A strategy that retries without limit.</returns>
        public static IRetryStrategy Linear(TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new LinearStrategy(initialDelay, delayStep);
        }

        /// <summary>
        /// Creates a strategy whose delay is multiplied by a fixed rate before each further retry.
        /// </summary>
        /// <param name="initialDelay">The delay before the first retry.</param>
        /// <param name="rate">The factor by which the delay grows for each further retry (optional). The default is 2.</param>
        /// <returns>A strategy that retries without limit.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rate"/> is less than 1.</exception>
        public static IRetryStrategy Exponential(TimeSpan initialDelay, double rate = 2.0)
        {
            return new ExponentialStrategy(initialDelay, rate);
        }

        /// <summary>
        /// Creates a strategy whose delay grows with the Fibonacci sequence, scaled by the initial delay.
        /// </summary>
        /// <param name="initialDelay">The delay before the first retry, which is also the amount scaled by the Fibonacci sequence for each further retry.</param>
        /// <returns>A strategy that retries without limit.</returns>
        public static IRetryStrategy Fibonacci(TimeSpan initialDelay)
        {
            return new FibonacciStrategy(initialDelay);
        }

        /// <summary>
        /// Creates a strategy whose delay grows with the Fibonacci sequence, scaled by a fixed step.
        /// </summary>
        /// <param name="initialDelay">The delay before the first retry.</param>
        /// <param name="delayStep">The amount scaled by the Fibonacci sequence and added to the initial delay for each further retry.</param>
        /// <returns>A strategy that retries without limit.</returns>
        public static IRetryStrategy Fibonacci(TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new FibonacciStrategy(initialDelay, delayStep);
        }
    }
}
