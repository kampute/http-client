// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers.Abstracts;
    using System;

    /// <summary>
    /// A scheduler that manages retry attempts with an exponentially increased (or decreased) delay.
    /// </summary>
    /// <remarks>
    /// The <see cref="ExponentialRetryScheduler"/> class allows for the exponential adjustment of the wait time between retries, controlled by 
    /// an initial delay, an exponential rate of change in the delay before retries, and a maximum number of attempts. This flexibility supports 
    /// scenarios ranging from aggressive backoff to more conservative or even decreasing delay strategies.
    /// </remarks>
    public sealed class ExponentialRetryScheduler : RetryScheduler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialRetryScheduler"/> class.
        /// </summary>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <param name="rate">The exponential rate of change in delay duration between retries. A value greater than 1.0 increases the delay, while a value less than 1.0 decreases it.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rate"/> is less than or equal to 0.</exception>            
        public ExponentialRetryScheduler(TimeSpan initialDelay, double rate)
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
        /// Gets the delay duration before the next attempt.
        /// </summary>
        public override TimeSpan Delay => TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * Math.Pow(Rate, Attempts));
    }
}
