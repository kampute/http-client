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
    /// A backoff strategy that implements an exponential change in the delay between retry attempts.
    /// </summary>
    /// <remarks>
    /// The <see cref="ExponentialRetryStrategy"/> class allows for the exponential adjustment of the wait time between retries, controlled by an initial 
    /// delay, an exponential rate for adjusting the delay before retries, and a maximum number of attempts. This flexibility supports scenarios ranging 
    /// from aggressive backoff to more conservative or even decreasing delay strategies.
    /// </remarks>
    public sealed class ExponentialRetryStrategy : RetryStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialRetryStrategy"/> class with a specified maximum number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts before giving up.</param>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <param name="rate">The exponential rate of change in delay duration between retries. A value greater than 1.0 increases the delay, while a value less than 1.0 decreases it.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1 or if <paramref name="rate"/> is less than or equal to 0.</exception>
        public ExponentialRetryStrategy(int maxAttempts, TimeSpan initialDelay, double rate)
            : base(maxAttempts)
        {
            if (rate <= 0)
                throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be greater than 0.");

            InitialDelay = initialDelay;
            Rate = rate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialRetryStrategy"/> class with a specified timeout duration for retries.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <param name="rate">The exponential rate of change in delay duration between retries. A value greater than 1.0 increases the delay, while a value less than 1.0 decreases it.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration or if <paramref name="rate"/> is less than or equal to 0.</exception>
        public ExponentialRetryStrategy(TimeSpan timeout, TimeSpan initialDelay, double rate)
            : base(timeout)
        {
            if (rate <= 0)
                throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be greater than 0.");

            InitialDelay = initialDelay;
            Rate = rate;
        }

        /// <summary>
        /// Gets the initial delay duration before the first retry attempt.
        /// </summary>
        public TimeSpan InitialDelay { get; }

        /// <summary>
        /// Gets the exponential rate of change in delay duration between retries.
        /// </summary>
        public double Rate { get; }

        /// <summary>
        /// Creates a scheduler that enforces an adjustable exponential delay between retries.
        /// </summary>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that schedules retries with an exponentially adjusted delay.</returns>
        protected override RetryScheduler CreateBaseScheduler() => new ExponentialRetryScheduler(InitialDelay, Rate);
    }
}
