// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement;
    using Kampute.HttpClient.RetryManagement.Strategies;
    using System;

    /// <summary>
    /// Provides a collection of factory methods for creating instances of different retry strategies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class offers various retry strategies to manage transient failures in distributed systems for flexible, use case-specific configuration.
    /// </para>
    /// <para>
    /// Strategies are ordered by increasing delay approach:
    /// <list type="number">
    /// <item>
    /// <term>None</term>
    /// <description>No retries. Best for critical operations where failures should be immediately addressed without retries.</description>
    /// </item>
    /// <item>
    /// <term>Once</term>
    /// <description>A single retry after a delay. Suitable for operations where one additional attempt may resolve a transient issue.</description>
    /// </item>
    /// <item>
    /// <term>Uniform</term>
    /// <description>Multiple retries with constant delays. Ideal for cases needing multiple attempts with predictable delays.</description>
    /// </item>
    /// <item>
    /// <term>Linear</term>
    /// <description>Multiple retries with delays increasing linearly. Optimal for reducing system load with gradually increasing wait times.</description>
    /// </item>
    /// <item>
    /// <term>Fibonacci</term>
    /// <description>Multiple retries with delays following the Fibonacci sequence. A balanced choice between aggressive and cautious retry pacing.</description>
    /// </item>
    /// <item>
    /// <term>Exponential</term>
    /// <description>Multiple retries with delays growing exponentially. For aggressively minimizing impact on systems by rapidly increasing wait times.</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// The <c>Dynamic</c> strategy stands apart, as its delay can vary based on the context of the failure. It tailors retry attempts to specific conditions, such
    /// as error type or system load, offering the flexibility to adapt retry logic for optimal outcomes. This approach is most useful in complex systems where a
    /// static retry strategy may not adequately address the nuances of different failure scenarios.
    /// </para>
    /// </remarks>
    public static class BackoffStrategies
    {
        /// <summary>
        /// Gets a strategy where no retry attempts are made.
        /// </summary>
        /// <value>
        /// An <see cref="IHttpBackoffProvider"/> that defines a retry strategy of no retry attempts.
        /// </value>
        /// <remarks>
        /// This strategy schedules no retry attempts, making it ideal for operations where failure handling is immediate or managed through other means.
        /// </remarks>
        public static IHttpBackoffProvider None { get; } = NoneStrategy.Instance.ToBackoffStrategy();

        /// <summary>
        /// Creates a strategy that performs a single retry attempt after the specified delay.
        /// </summary>
        /// <param name="delay">The delay before the single retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy of a single attempt after a specified delay.</returns>
        /// <remarks>
        /// This strategy performs a single retry attempt after the specified delay. It is suitable for operations where one additional attempt may resolve
        /// a transient issue.
        /// </remarks>
        public static IHttpBackoffProvider Once(TimeSpan delay)
        {
            return new UniformStrategy(delay).WithMaxAttempts(1).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs a single retry attempt after the specified date and time.
        /// </summary>
        /// <param name="after">The date and time after which the single retry attempt will be made.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy of a single attempt after a specified date and time.</returns>
        /// <remarks>
        /// This strategy schedules a single retry attempt for a specified future point in time, ensuring operations are retried when certain
        /// conditions are likely met. If the specified time has already passed, it immediately schedules the retry attempt.
        /// </remarks>
        public static IHttpBackoffProvider Once(DateTimeOffset after)
        {
            return new UniformStrategy(after - DateTimeOffset.UtcNow).WithMaxAttempts(1).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with a constant delay between each attempt, up to a specified maximum number of retry
        /// attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="delay">The constant delay between each retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy of multiple attempts with a constant delay between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with a constant delay between each attempt. It is ideal for cases needing multiple attempts with
        /// predictable delays.
        /// </remarks>
        public static IHttpBackoffProvider Uniform(uint maxAttempts, TimeSpan delay)
        {
            return new UniformStrategy(delay).WithMaxAttempts(maxAttempts).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with a constant delay between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="delay">The constant delay between each retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy of multiple attempts with a constant delay between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with a constant delay between each attempt, up to a specified timeout. It is ideal for cases needing
        /// multiple attempts with predictable delays, but with a maximum time limit for retrying.
        /// </remarks>
        public static IHttpBackoffProvider Uniform(TimeSpan timeout, TimeSpan delay)
        {
            return new UniformStrategy(delay).WithTimeout(timeout).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified maximum number of
        /// retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="delayStep">The amount by which the delay increases for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy of multiple attempts with a constant delay between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing linearly between each attempt. It is optimal for reducing system load with
        /// gradually increasing wait times.
        /// </remarks>
        public static IHttpBackoffProvider Linear(uint maxAttempts, TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new LinearStrategy(initialDelay, delayStep).WithMaxAttempts(maxAttempts).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="delayStep">The amount by which the delay increases for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays increasing linearly between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified timeout. It is optimal for
        /// reducing system load with gradually increasing wait times, while enforcing a maximum time limit for retrying.
        /// </remarks>
        public static IHttpBackoffProvider Linear(TimeSpan timeout, TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new LinearStrategy(initialDelay, delayStep).WithTimeout(timeout).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified maximum number of
        /// retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays increasing linearly between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing linearly between each attempt. It is optimal for reducing system load with
        /// gradually increasing wait times.
        /// </remarks>
        public static IHttpBackoffProvider Linear(uint maxAttempts, TimeSpan initialDelay)
        {
            return new LinearStrategy(initialDelay).WithMaxAttempts(maxAttempts).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays increasing linearly between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified timeout. It is optimal for
        /// reducing system load with gradually increasing wait times, while enforcing a maximum time limit for retrying.
        /// </remarks>
        public static IHttpBackoffProvider Linear(TimeSpan timeout, TimeSpan initialDelay)
        {
            return new LinearStrategy(initialDelay).WithTimeout(timeout).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing exponentially between each attempt, up to a specified maximum number
        /// of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="rate">The rate at which the delay increases for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays increasing exponentially between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing exponentially between each attempt. It is suitable for aggressively minimizing
        /// the impact on systems by rapidly increasing wait times between retry attempts.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rate"/> is less than 1.</exception>
        public static IHttpBackoffProvider Exponential(uint maxAttempts, TimeSpan initialDelay, double rate = 2.0)
        {
            return new ExponentialStrategy(initialDelay, rate).WithMaxAttempts(maxAttempts).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing exponentially between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="rate">The rate at which the delay increases for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays increasing exponentially between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing exponentially between each attempt, up to a specified timeout. It is suitable
        /// for aggressively minimizing the impact on systems by rapidly increasing wait times between retry attempts, while enforcing a maximum time limit for
        /// retrying.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rate"/> is less than 1.</exception>
        public static IHttpBackoffProvider Exponential(TimeSpan timeout, TimeSpan initialDelay, double rate = 2.0)
        {
            return new ExponentialStrategy(initialDelay, rate).WithTimeout(timeout).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified maximum
        /// number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time that is scaled by the Fibonacci sequence and added to the initial delay for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays following the Fibonacci sequence between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays following the Fibonacci sequence between each attempt. It provides a balanced choice between
        /// aggressive and cautious retry pacing, suitable for a wide range of scenarios.
        /// </remarks>
        public static IHttpBackoffProvider Fibonacci(uint maxAttempts, TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new FibonacciStrategy(initialDelay, delayStep).WithMaxAttempts(maxAttempts).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time that is scaled by the Fibonacci sequence and added to the initial delay for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays following the Fibonacci sequence between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified timeout. It provides
        /// a balanced choice between aggressive and cautious retry pacing, suitable for a wide range of scenarios, while enforcing a maximum time limit for retrying.
        /// </remarks>
        public static IHttpBackoffProvider Fibonacci(TimeSpan timeout, TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new FibonacciStrategy(initialDelay, delayStep).WithTimeout(timeout).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified maximum
        /// number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays following the Fibonacci sequence between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays following the Fibonacci sequence between each attempt. It provides a balanced choice between
        /// aggressive and cautious retry pacing, suitable for a wide range of scenarios.
        /// </remarks>
        public static IHttpBackoffProvider Fibonacci(uint maxAttempts, TimeSpan initialDelay)
        {
            return new FibonacciStrategy(initialDelay).WithMaxAttempts(maxAttempts).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> that defines a retry strategy with delays following the Fibonacci sequence between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified timeout. It provides
        /// a balanced choice between aggressive and cautious retry pacing, suitable for a wide range of scenarios, while enforcing a maximum time limit for retrying.
        /// </remarks>
        public static IHttpBackoffProvider Fibonacci(TimeSpan timeout, TimeSpan initialDelay)
        {
            return new FibonacciStrategy(initialDelay).WithTimeout(timeout).ToBackoffStrategy();
        }

        /// <summary>
        /// Creates an instance of <see cref="DynamicBackoffStrategy"/> with a dynamic strategy factory based on the context of a failed HTTP request.
        /// </summary>
        /// <param name="strategyFactory">A factory function that creates <see cref="IRetryStrategy"/> instances based on the failed HTTP request context.</param>
        /// <returns>An instance of <see cref="DynamicBackoffStrategy"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategyFactory"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This strategy offers the highest flexibility by dynamically scheduling retries based on the specific context of a failure. It adapts to the nature of
        /// encountered errors, making it ideal for complex systems with varied types of transient failures that cannot be effectively handled by a static retry strategy.
        /// </remarks>
        public static IHttpBackoffProvider Dynamic(Func<HttpRequestErrorContext, IRetryStrategy> strategyFactory)
        {
            return new DynamicBackoffStrategy(strategyFactory);
        }

        /// <summary>
        /// Creates an instance of <see cref="DynamicBackoffStrategy"/> with a dynamic scheduler factory based on the context of a failed HTTP request.
        /// </summary>
        /// <param name="schedulerFactory">A factory function that creates <see cref="IRetryScheduler"/> instances based on the failed HTTP request context.</param>
        /// <returns>An instance of <see cref="DynamicBackoffStrategy"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedulerFactory"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This strategy offers the highest flexibility by dynamically scheduling retries based on the specific context of a failure. It adapts to the nature of
        /// encountered errors, making it ideal for complex systems with varied types of transient failures that cannot be effectively handled by a static retry strategy.
        /// </remarks>
        public static IHttpBackoffProvider Dynamic(Func<HttpRequestErrorContext, IRetryScheduler> schedulerFactory)
        {
            return new DynamicBackoffStrategy(schedulerFactory);
        }
    }
}
