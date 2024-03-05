// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers.Abstracts;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A scheduler that introduces a maximum attempt limit on top of an existing retry scheduler.
    /// </summary>
    /// <remarks>
    /// This scheduler wraps another scheduler and adds an attempt limit constraint to the retry logic. If the number of retry attempts 
    /// reaches the specified maximum limit, further retries are aborted. This is useful to prevent excessive retries in scenarios where 
    /// a fixed number of attempts is preferred.
    /// </remarks>
    public sealed class AttemptLimitRetryScheduler : RetryScheduler
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="AttemptLimitRetryScheduler"/> class with the specified underlying scheduler and 
        /// maximum attempt limit.
        /// </summary>
        /// <param name="scheduler">The base retry scheduler to which attempt limit handling will be added.</param>
        /// <param name="maxAttempts">The maximum number of retry attempts before giving up.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scheduler"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1.</exception>            
        public AttemptLimitRetryScheduler(RetryScheduler scheduler, int maxAttempts)
        {
            if (scheduler is null)
                throw new ArgumentNullException(nameof(scheduler));
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1.");

            BaseScheduler = scheduler;
            MaxAttempts = maxAttempts;
        }

        /// <summary>
        /// Gets the base retry scheduler to which timeout handling is added.
        /// </summary>
        public RetryScheduler BaseScheduler { get; }

        /// <summary>
        /// Gets the maximum number of retry attempts before giving up.
        /// </summary>
        public int MaxAttempts { get; }

        /// <summary>
        /// Gets the number of retry attempts that have been made.
        /// </summary>
        public override int Attempts => BaseScheduler.Attempts;

        /// <summary>
        /// Gets the total elapsed time since the retry attempts was started.
        /// </summary>
        public override TimeSpan Elapsed => BaseScheduler.Elapsed;

        /// <summary>
        /// Gets the delay duration before the next attempt.
        /// </summary>
        public override TimeSpan Delay => BaseScheduler.Delay;

        /// <summary>
        /// Waits for the appropriate time before the next retry attempt, and determines if a retry should be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        /// <returns>A task that resolves to <c>true</c> if a retry should be attempted; otherwise, <c>false</c>.</returns>
        /// <exception cref="TaskCanceledException">Thrown if the wait operation is canceled.</exception>
        public override Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            return Attempts < MaxAttempts
                ? BaseScheduler.WaitAsync(cancellationToken)
                : Task.FromResult(false);
        }

        /// <summary>
        /// Resets the internal state of the scheduler to its initial condition.
        /// </summary>
        public override void Reset() => BaseScheduler.Reset();
    }
}
