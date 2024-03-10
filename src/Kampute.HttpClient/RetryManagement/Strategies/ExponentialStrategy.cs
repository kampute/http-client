// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryManagement.Strategies
{
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// A retry strategy that exponentially increases the delay before each retry attempt.
    /// </summary>
    /// <remarks>
    /// The <see cref="ExponentialStrategy"/> class calculates the delay between retry attempts by starting with an initial delay and then increasing it exponentially
    /// with each subsequent retry. This strategy is effective in scenarios where the load on the underlying systems needs to be progressively reduced in the
    /// face of ongoing failures, or where it is beneficial to wait longer between attempts to increase the chance of success.
    /// </remarks>
    public sealed class ExponentialStrategy : IRetryStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialStrategy"/> class with a specified initial delay and exponential rate.
        /// </summary>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <param name="rate">The rate at which the delay duration increases exponentially between retries.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rate"/> is less than 1.</exception>
        public ExponentialStrategy(TimeSpan initialDelay, double rate)
        {
            if (rate < 1.0)
                throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be at least 1.");

            InitialDelay = initialDelay;
            Rate = rate;
        }

        /// <summary>
        /// Gets the initial delay duration before the first retry attempt.
        /// </summary>
        public TimeSpan InitialDelay { get; }

        /// <summary>
        /// Gets the rate at which the delay duration increases exponentially between retries.
        /// </summary>
        public double Rate { get; }

        /// <summary>
        /// Calculates the delay for the next retry attempt, exponentially increasing based on the number of attempts made so far.
        /// </summary>
        /// <param name="elapsed">The total time elapsed since the start of retry attempts. This parameter is ignored in this implementation.</param>
        /// <param name="attempts">The number of retry attempts made so far.</param>
        /// <param name="delay">When this method returns, contains the calculated delay for the next retry attempt. This parameter is passed uninitialized.</param>
        /// <returns>Always returns <c>true</c>, indicating that a retry attempt should be made after the calculated <paramref name="delay"/>.</returns>
        public bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay)
        {
            var millisecondsDelay = InitialDelay.TotalMilliseconds * Math.Pow(Rate, attempts);
            delay = TimeSpan.FromMilliseconds(millisecondsDelay);
            return true;
        }
    }
}
