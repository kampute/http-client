// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryStrategies;
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
        /// Gets an instance of <see cref="ZeroRetryStrategy"/> that represents a strategy where no retry attempts are made.
        /// </summary>
        /// <remarks>
        /// This strategy schedules no retry attempts, making it ideal for operations where failure handling is immediate or managed through other means.
        /// </remarks>
        public static ZeroRetryStrategy None => ZeroRetryStrategy.Instance;

        /// <summary>
        /// Creates an instance of <see cref="UniformRetryStrategy"/> with a specified delay for a single retry attempt.
        /// </summary>
        /// <param name="delay">The fixed delay before the retry attempt.</param>
        /// <returns>An instance of <see cref="UniformRetryStrategy"/> configured to attempt a single retry after the specified delay.</returns>
        /// <remarks>
        /// This strategy schedules a single retry attempt after a specified fixed delay.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniformRetryStrategy Once(TimeSpan delay) => new(1, delay);

        /// <summary>
        /// Creates an instance of <see cref="UniformRetryStrategy"/> that schedules exactly one retry attempt to occur after a specified <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="after">The <see cref="DateTimeOffset"/> after which a single retry attempt should be made.</param>
        /// <returns>An instance of <see cref="UniformRetryStrategy"/> configured to attempt a single retry after the specified time.</returns>
        /// <remarks>
        /// This strategy schedules a single retry attempt for a specified future point in time, ensuring operations are retried when certain 
        /// conditions are likely met. If the specified time has already passed, it immediately schedules the retry attempt.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniformRetryStrategy Once(DateTimeOffset after) => new(1, after - DateTimeOffset.UtcNow);

        /// <summary>
        /// Creates an instance of <see cref="UniformRetryStrategy"/> with a specified maximum number of retry attempts and a fixed delay between retries.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="delay">The fixed delay duration between retry attempts.</param>
        /// <returns>An instance of <see cref="UniformRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1.</exception>
        /// <remarks>
        /// This strategy schedules a specific number of retry attempts with a uniform delay between each, ideal for scenarios where a consistent retry 
        /// interval is beneficial.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniformRetryStrategy Uniform(int maxAttempts, TimeSpan delay) => new(maxAttempts, delay);

        /// <summary>
        /// Creates an instance of <see cref="UniformRetryStrategy"/> with a specified timeout duration for retries and a fixed delay between retries.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <param name="delay">The fixed delay duration between retry attempts.</param>
        /// <returns>An instance of <see cref="UniformRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration.</exception>
        /// <remarks>
        /// This strategy schedules retries within a predetermined total timeout period with a uniform delay between attempts.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniformRetryStrategy Uniform(TimeSpan timeout, TimeSpan delay) => new(timeout, delay);

        /// <summary>
        /// Creates an instance of <see cref="LinearRetryStrategy"/> with a specified maximum number of retry attempts, initial delay, and delay increment step.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The initial delay before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time by which the delay increases with each retry.</param>
        /// <returns>An instance of <see cref="LinearRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1.</exception>
        /// <remarks>
        /// This strategy schedules a specific number of retry attempts where the delay increases linearly after each attempt.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LinearRetryStrategy Linear(int maxAttempts, TimeSpan initialDelay, TimeSpan delayStep) => new(maxAttempts, initialDelay, delayStep);

        /// <summary>
        /// Creates an instance of <see cref="LinearRetryStrategy"/> with a specified timeout duration for retries, initial delay, and delay increment step.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <param name="initialDelay">The initial delay before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time by which the delay increases with each retry.</param>
        /// <returns>An instance of <see cref="LinearRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration.</exception>
        /// <remarks>
        /// This strategy schedules retries within a predetermined total timeout period where the delay increases linearly after each attempt.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LinearRetryStrategy Linear(TimeSpan timeout, TimeSpan initialDelay, TimeSpan delayStep) => new(timeout, initialDelay, delayStep);

        /// <summary>
        /// Creates an instance of <see cref="ExponentialRetryStrategy"/> with a specified maximum number of retry attempts, initial delay, and exponential rate.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The initial delay before the first retry attempt.</param>
        /// <param name="rate">The rate at which the delay increases exponentially.</param>
        /// <returns>An instance of <see cref="ExponentialRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1 or <paramref name="rate"/> is less than or equal to 0.</exception>
        /// <remarks>
        /// This strategy schedules a specific number of retry attempts where the delay increases exponentially after each attempt.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExponentialRetryStrategy Exponential(int maxAttempts, TimeSpan initialDelay, double rate = 2.0) => new(maxAttempts, initialDelay, rate);

        /// <summary>
        /// Creates an instance of <see cref="ExponentialRetryStrategy"/> with a specified timeout duration for retries, initial delay, and exponential rate.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <param name="initialDelay">The initial delay before the first retry attempt.</param>
        /// <param name="rate">The rate at which the delay increases exponentially.</param>
        /// <returns>An instance of <see cref="ExponentialRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration or <paramref name="rate"/> is less than or equal to 0.</exception>
        /// <remarks>
        /// This strategy schedules retries within a predetermined total timeout period where the delay increases exponentially after each attempt.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExponentialRetryStrategy Exponential(TimeSpan timeout, TimeSpan initialDelay, double rate = 2.0) => new(timeout, initialDelay, rate);

        /// <summary>
        /// Creates an instance of <see cref="FibonacciRetryStrategy"/> with a specified maximum number of retry attempts and an initial delay.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The initial delay before the first retry attempt.</param>
        /// <returns>An instance of <see cref="FibonacciRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1.</exception>
        /// <remarks>
        /// This strategy schedules a specific number of retry attempts where the delay increases based on the Fibonacci sequence after each attempt.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FibonacciRetryStrategy Fibonacci(int maxAttempts, TimeSpan initialDelay) => new(maxAttempts, initialDelay);

        /// <summary>
        /// Creates an instance of <see cref="FibonacciRetryStrategy"/> with a specified timeout duration for retries and an initial delay.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <param name="initialDelay">The initial delay before the first retry attempt.</param>
        /// <returns>An instance of <see cref="FibonacciRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration.</exception>
        /// <remarks>
        /// This strategy schedules retries within a predetermined total timeout period where the delay increases based on the Fibonacci sequence after each attempt.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FibonacciRetryStrategy Fibonacci(TimeSpan timeout, TimeSpan initialDelay) => new(timeout, initialDelay);

        /// <summary>
        /// Creates an instance of <see cref="DynamicRetryStrategy"/> with a dynamic strategy factory based on the context of a failed HTTP request.
        /// </summary>
        /// <param name="strategyFactory">A factory function that creates <see cref="IRetryStrategy"/> instances based on the failed HTTP request context.</param>
        /// <returns>An instance of <see cref="DynamicRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategyFactory"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This strategy offers the highest flexibility by dynamically scheduling retries based on the specific context of a failure. It adapts to the nature of 
        /// encountered errors, making it ideal for complex systems with varied types of transient failures that cannot be effectively handled by a static retry strategy.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicRetryStrategy Dynamic(Func<HttpRequestErrorContext, IRetryStrategy> strategyFactory) => new(strategyFactory);

        /// <summary>
        /// Creates an instance of <see cref="DynamicRetryStrategy"/> with a dynamic scheduler factory based on the context of a failed HTTP request.
        /// </summary>
        /// <param name="schedulerFactory">A factory function that creates <see cref="IRetryScheduler"/> instances based on the failed HTTP request context.</param>
        /// <returns>An instance of <see cref="DynamicRetryStrategy"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedulerFactory"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This strategy offers the highest flexibility by dynamically scheduling retries based on the specific context of a failure. It adapts to the nature of 
        /// encountered errors, making it ideal for complex systems with varied types of transient failures that cannot be effectively handled by a static retry strategy.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicRetryStrategy Dynamic(Func<HttpRequestErrorContext, IRetryScheduler> schedulerFactory) => new(schedulerFactory);
    }
}
