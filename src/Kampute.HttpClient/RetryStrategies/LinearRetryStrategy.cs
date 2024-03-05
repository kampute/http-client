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
    /// A backoff strategy that implements a linear change in the delay between retry attempts. 
    /// </summary>
    /// <remarks>
    /// The <see cref="LinearRetryStrategy"/> class adjusts the wait time between retries linearly, based on an initial delay and a fixed step. 
    /// This approach allows for a predictable change in delay, which can be useful in scenarios where controlling the pace of retries 
    /// is important.
    /// </remarks>
    public sealed class LinearRetryStrategy : RetryStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearRetryStrategy"/> class with a specified maximum number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts before giving up.</param>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time by which the delay is incremented with each retry.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1.</exception>
        public LinearRetryStrategy(int maxAttempts, TimeSpan initialDelay, TimeSpan delayStep)
            : base(maxAttempts)
        {
            InitialDelay = initialDelay;
            DelayStep = delayStep;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearRetryStrategy"/> class with a specified timeout duration for retries.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time by which the delay is incremented with each retry.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration.</exception>
        public LinearRetryStrategy(TimeSpan timeout, TimeSpan initialDelay, TimeSpan delayStep)
            : base(timeout)
        {
            InitialDelay = initialDelay;
            DelayStep = delayStep;
        }

        /// <summary>
        /// Gets the initial delay duration before the first retry attempt.
        /// </summary>
        public TimeSpan InitialDelay { get; }

        /// <summary>
        /// Gets the fixed amount of time by which the delay is incremented with each retry.
        /// </summary>
        public TimeSpan DelayStep { get; }

        /// <summary>
        /// Creates a scheduler that enforces a linearly adjusted delay between retries.
        /// </summary>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that schedules retries with a linearly adjusted delay.</returns>
        protected override RetryScheduler CreateBaseScheduler() => new LinearRetryScheduler(InitialDelay, DelayStep);
    }
}
