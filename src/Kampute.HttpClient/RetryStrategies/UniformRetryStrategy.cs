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
    /// A backoff strategy that implements a uniform delay between retry attempts for operations.
    /// </summary>
    /// <remarks>
    /// The <see cref="UniformRetryStrategy"/> class provides a retry mechanism with a constant wait time between retries. This approach is 
    /// useful for scenarios where a predictable retry pattern is desired.
    /// </remarks>
    public sealed class UniformRetryStrategy : RetryStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UniformRetryStrategy"/> class with a specified maximum number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts before giving up.</param>
        /// <param name="delay">The delay duration between retry attempts.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1.</exception>
        public UniformRetryStrategy(int maxAttempts, TimeSpan delay)
            : base(maxAttempts)
        {
            Delay = delay;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniformRetryStrategy"/> class with a specified timeout duration for retries.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <param name="delay">The delay duration between retry attempts.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration.</exception>
        public UniformRetryStrategy(TimeSpan timeout, TimeSpan delay)
            : base(timeout)
        {
            Delay = delay;
        }

        /// <summary>
        /// Gets the delay duration between retries.
        /// </summary>
        public TimeSpan Delay { get; }

        /// <summary>
        /// Creates a scheduler that enforces a fixed delay between retries.
        /// </summary>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that schedules retries with a fixed delay.</returns>
        protected override RetryScheduler CreateBaseScheduler() => new UniformRetryScheduler(Delay);
    }
}