﻿// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement;
    using Kampute.HttpClient.RetryManagement.Strategies;
    using System;
    using System.Runtime.CompilerServices;

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
        /// An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy of no retry attempts.
        /// </value>
        /// <remarks>
        /// This strategy schedules no retry attempts, making it ideal for operations where failure handling is immediate or managed through other means.
        /// </remarks>
        public static IRetrySchedulerFactory None { get; } = NoneStrategy.Instance.ToSchedulerFactory();

        /// <summary>
        /// Creates a strategy that performs a single retry attempt after the specified delay.
        /// </summary>
        /// <param name="delay">The delay before the single retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy of a single attempt after a specified delay.</returns>
        /// <remarks>
        /// This strategy performs a single retry attempt after the specified delay. It is suitable for operations where one additional attempt may resolve
        /// a transient issue.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Once(TimeSpan delay)
        {
            return new UniformStrategy(delay).MaxAttempts(1).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs a single retry attempt after the specified date and time.
        /// </summary>
        /// <param name="after">The date and time after which the single retry attempt will be made.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy of a single attempt after a specified date and time.</returns>
        /// <remarks>
        /// This strategy schedules a single retry attempt for a specified future point in time, ensuring operations are retried when certain 
        /// conditions are likely met. If the specified time has already passed, it immediately schedules the retry attempt.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Once(DateTimeOffset after)
        {
            return new UniformStrategy(after - DateTimeOffset.UtcNow).MaxAttempts(1).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with a constant delay between each attempt, up to a specified maximum number of retry
        /// attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="delay">The constant delay between each retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy of multiple attempts with a constant delay between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with a constant delay between each attempt. It is ideal for cases needing multiple attempts with
        /// predictable delays.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Uniform(uint maxAttempts, TimeSpan delay)
        {
            return new UniformStrategy(delay).MaxAttempts(maxAttempts).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with a constant delay between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="delay">The constant delay between each retry attempt.</param>
        /// <remarks>
        /// This strategy performs multiple retry attempts with a constant delay between each attempt, up to a specified timeout. It is ideal for cases needing
        /// multiple attempts with predictable delays, but with a maximum time limit for retrying.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Uniform(TimeSpan timeout, TimeSpan delay)
        {
            return new UniformStrategy(delay).Timeout(timeout).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified maximum number of
        /// retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="delayStep">The amount by which the delay increases for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy of multiple attempts with a constant delay between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing linearly between each attempt. It is optimal for reducing system load with
        /// gradually increasing wait times.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Linear(uint maxAttempts, TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new LinearStrategy(initialDelay, delayStep).MaxAttempts(maxAttempts).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="delayStep">The amount by which the delay increases for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays increasing linearly between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified timeout. It is optimal for
        /// reducing system load with gradually increasing wait times, while enforcing a maximum time limit for retrying.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Linear(TimeSpan timeout, TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new LinearStrategy(initialDelay, delayStep).Timeout(timeout).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified maximum number of
        /// retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays increasing linearly between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing linearly between each attempt. It is optimal for reducing system load with
        /// gradually increasing wait times.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Linear(uint maxAttempts, TimeSpan initialDelay)
        {
            return new LinearStrategy(initialDelay).MaxAttempts(maxAttempts).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays increasing linearly between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing linearly between each attempt, up to a specified timeout. It is optimal for
        /// reducing system load with gradually increasing wait times, while enforcing a maximum time limit for retrying.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Linear(TimeSpan timeout, TimeSpan initialDelay)
        {
            return new LinearStrategy(initialDelay).Timeout(timeout).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing exponentially between each attempt, up to a specified maximum number
        /// of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="rate">The rate at which the delay increases for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays increasing exponentially between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing exponentially between each attempt. It is suitable for aggressively minimizing
        /// the impact on systems by rapidly increasing wait times between retry attempts.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rate"/> is less than 1.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Exponential(uint maxAttempts, TimeSpan initialDelay, double rate = 2.0)
        {
            return new ExponentialStrategy(initialDelay, rate).MaxAttempts(maxAttempts).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays increasing exponentially between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="rate">The rate at which the delay increases for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays increasing exponentially between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays increasing exponentially between each attempt, up to a specified timeout. It is suitable
        /// for aggressively minimizing the impact on systems by rapidly increasing wait times between retry attempts, while enforcing a maximum time limit for
        /// retrying.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rate"/> is less than 1.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Exponential(TimeSpan timeout, TimeSpan initialDelay, double rate = 2.0)
        {
            return new ExponentialStrategy(initialDelay, rate).Timeout(timeout).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified maximum
        /// number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time that is scaled by the Fibonacci sequence and added to the initial delay for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays following the Fibonacci sequence between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays following the Fibonacci sequence between each attempt. It provides a balanced choice between
        /// aggressive and cautious retry pacing, suitable for a wide range of scenarios.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Fibonacci(uint maxAttempts, TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new FibonacciStrategy(initialDelay, delayStep).MaxAttempts(maxAttempts).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time that is scaled by the Fibonacci sequence and added to the initial delay for each subsequent retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays following the Fibonacci sequence between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified timeout. It provides
        /// a balanced choice between aggressive and cautious retry pacing, suitable for a wide range of scenarios, while enforcing a maximum time limit for retrying.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Fibonacci(TimeSpan timeout, TimeSpan initialDelay, TimeSpan delayStep)
        {
            return new FibonacciStrategy(initialDelay, delayStep).Timeout(timeout).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified maximum
        /// number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays following the Fibonacci sequence between each attempt, up to a specified maximum number of retry attempts.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays following the Fibonacci sequence between each attempt. It provides a balanced choice between
        /// aggressive and cautious retry pacing, suitable for a wide range of scenarios.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Fibonacci(uint maxAttempts, TimeSpan initialDelay)
        {
            return new FibonacciStrategy(initialDelay).MaxAttempts(maxAttempts).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates a strategy that performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to spend retrying.</param>
        /// <param name="initialDelay">The delay before the first retry attempt.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines a retry strategy with delays following the Fibonacci sequence between each attempt, up to a specified timeout.</returns>
        /// <remarks>
        /// This strategy performs multiple retry attempts with delays following the Fibonacci sequence between each attempt, up to a specified timeout. It provides
        /// a balanced choice between aggressive and cautious retry pacing, suitable for a wide range of scenarios, while enforcing a maximum time limit for retrying.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Fibonacci(TimeSpan timeout, TimeSpan initialDelay)
        {
            return new FibonacciStrategy(initialDelay).Timeout(timeout).ToSchedulerFactory();
        }

        /// <summary>
        /// Creates an instance of <see cref="DynamicRetrySchedulerFactory"/> with a dynamic strategy factory based on the context of a failed HTTP request.
        /// </summary>
        /// <param name="strategyFactory">A factory function that creates <see cref="IRetryStrategy"/> instances based on the failed HTTP request context.</param>
        /// <returns>An instance of <see cref="DynamicRetrySchedulerFactory"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategyFactory"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This strategy offers the highest flexibility by dynamically scheduling retries based on the specific context of a failure. It adapts to the nature of 
        /// encountered errors, making it ideal for complex systems with varied types of transient failures that cannot be effectively handled by a static retry strategy.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Dynamic(Func<HttpRequestErrorContext, IRetryStrategy> strategyFactory)
        {
            return new DynamicRetrySchedulerFactory(strategyFactory);
        }

        /// <summary>
        /// Creates an instance of <see cref="DynamicRetrySchedulerFactory"/> with a dynamic scheduler factory based on the context of a failed HTTP request.
        /// </summary>
        /// <param name="schedulerFactory">A factory function that creates <see cref="IRetryScheduler"/> instances based on the failed HTTP request context.</param>
        /// <returns>An instance of <see cref="DynamicRetrySchedulerFactory"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedulerFactory"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This strategy offers the highest flexibility by dynamically scheduling retries based on the specific context of a failure. It adapts to the nature of 
        /// encountered errors, making it ideal for complex systems with varied types of transient failures that cannot be effectively handled by a static retry strategy.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRetrySchedulerFactory Dynamic(Func<HttpRequestErrorContext, IRetryScheduler> schedulerFactory)
        {
            return new DynamicRetrySchedulerFactory(schedulerFactory);
        }
    }
}
