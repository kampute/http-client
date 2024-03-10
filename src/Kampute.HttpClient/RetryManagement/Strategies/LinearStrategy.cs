// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryManagement.Strategies
{
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// A retry strategy that linearly increases the delay before each retry attempt.
    /// </summary>
    /// <remarks>
    /// The <see cref="LinearStrategy"/> class calculates the delay between retry attempts by starting with an initial delay and then increasing it linearly with
    /// each subsequent retry. This linear increment strategy offers a controlled escalation of wait times, making it suitable for scenarios where
    /// gradually backing off is preferred to reduce the load on resources or to increase the chances of a successful retry under improving conditions.
    /// </remarks>
    public sealed class LinearStrategy : IRetryStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearStrategy"/> class with a specified initial delay and step increment.
        /// </summary>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        /// <param name="delayStep">The fixed amount of time by which the delay is incremented for each subsequent retry.</param>
        public LinearStrategy(TimeSpan initialDelay, TimeSpan delayStep)
        {
            InitialDelay = initialDelay;
            DelayStep = delayStep;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearStrategy"/> class with a specified initial delay.
        /// </summary>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        public LinearStrategy(TimeSpan initialDelay)
        {
            InitialDelay = initialDelay;
            DelayStep = initialDelay;
        }

        /// <summary>
        /// Gets the initial delay duration before the first retry attempt.
        /// </summary>
        public TimeSpan InitialDelay { get; }

        /// <summary>
        /// Gets the fixed amount of time by which the delay is increased with each retry attempt.
        /// </summary>
        public TimeSpan DelayStep { get; }

        /// <summary>
        /// Calculates the delay for the next retry attempt, linearly increasing based on the number of attempts made so far.
        /// </summary>
        /// <param name="elapsed">The total time elapsed since the start of retry attempts. This parameter is ignored in this implementation.</param>
        /// <param name="attempts">The number of retry attempts made so far.</param>
        /// <param name="delay">When this method returns, contains the calculated delay for the next retry attempt. This parameter is passed uninitialized.</param>
        /// <returns>Always returns <c>true</c>, indicating that a retry attempt should be made after the calculated <paramref name="delay"/>.</returns>
        public bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay)
        {
            delay = InitialDelay + TimeSpan.FromTicks(DelayStep.Ticks * attempts);
            return true;
        }
    }
}
