// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryStrategies
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetrySchedulers;
    using Kampute.HttpClient.RetrySchedulers.Abstracts;
    using Kampute.HttpClient.RetryStrategies.Abstracts;
    using System;

    /// <summary>
    /// A backoff strategy that implements a Fibonacci increase in the delay between retry attempts.
    /// </summary>
    /// <remarks>
    /// The <see cref="FibonacciRetryStrategy"/> class uses the Fibonacci sequence to adjust the wait time between retries, starting 
    /// with an initial delay and scaling the delay between subsequent attempts according to Fibonacci numbers. This approach provides 
    /// a more moderate and controlled increase in delay times compared to <see cref="ExponentialRetryStrategy"/> strategy, making it suitable 
    /// for scenarios where a less aggressive increase in delay is desired.
    /// </remarks>
    public sealed class FibonacciRetryStrategy : RetryStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FibonacciRetryStrategy"/> class with a specified maximum number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts before giving up.</param>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1.</exception>
        public FibonacciRetryStrategy(int maxAttempts, TimeSpan initialDelay)
            : base(maxAttempts)
        {
            InitialDelay = initialDelay;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FibonacciRetryStrategy"/> class with a specified timeout duration for retries.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration.</exception>
        public FibonacciRetryStrategy(TimeSpan timeout, TimeSpan initialDelay)
            : base(timeout)
        {
            InitialDelay = initialDelay;
        }

        /// <summary>
        /// Gets the initial delay duration before the first retry attempt.
        /// </summary>
        public TimeSpan InitialDelay { get; }

        /// <summary>
        /// Creates a scheduler that enforces a Fibonacci-adjusted delay between retries.
        /// </summary>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that schedules retries with a Fibonacci-adjusted delay.</returns>
        protected override RetryScheduler CreateBaseScheduler() => new FibonacciRetryScheduler(InitialDelay);
    }
}
