// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers.Abstracts;
    using System;

    /// <summary>
    /// A scheduler that manages retry attempts with a linearly increased (or decreased) delay.
    /// </summary>
    /// <remarks>
    /// The <see cref="LinearRetryScheduler"/> class adjusts the wait time between retries linearly, based on an initial delay and a 
    /// fixed step. This approach allows for a predictable change in delay, which can be useful in scenarios where controlling the pace 
    /// of retries is important.
    /// </remarks>
    public sealed class LinearRetryScheduler : RetryScheduler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearRetryScheduler"/> class.
        /// </summary>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time by which the delay is incremented with each retry.</param>
        public LinearRetryScheduler(TimeSpan initialDelay, TimeSpan delayStep)
        {
            InitialDelay = initialDelay;
            DelayStep = delayStep;
        }

        /// <summary>
        /// Gets the initial delay duration before the first retry attempt.
        /// </summary>
        public TimeSpan InitialDelay { get; }

        /// <summary>
        /// Gets the fixed amount of time by which the delay is increased with each retry.
        /// </summary>
        public TimeSpan DelayStep { get; }

        /// <summary>
        /// Gets the delay duration before the next attempt.
        /// </summary>
        public override TimeSpan Delay => TimeSpan.FromTicks(InitialDelay.Ticks + DelayStep.Ticks * Attempts);
    }
}
