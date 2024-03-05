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
    /// A scheduler that introduces a timeout mechanism on top of an existing retry scheduler.
    /// </summary>
    /// <remarks>
    /// This scheduler wraps another scheduler and adds a timeout constraint to the retry logic. If the cumulative wait time exceeds 
    /// the specified timeout, further retries are aborted. This mechanism is particularly useful in scenarios where operations must 
    /// complete within a certain time frame to avoid excessive delays
    /// </remarks>
    public sealed class TimeLimitRetryScheduler : RetryScheduler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeLimitRetryScheduler"/> class with the specified underlying scheduler and timeout.
        /// </summary>
        /// <param name="scheduler">The base retry scheduler to which timeout handling will be added.</param>
        /// <param name="timeout">The maximum duration to attempt retries.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scheduler"/> is <c>null</c>.</exception>
        public TimeLimitRetryScheduler(RetryScheduler scheduler, TimeSpan timeout)
        {
            BaseScheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Timeout = timeout;
        }

        /// <summary>
        /// Gets the base retry scheduler to which timeout handling is added.
        /// </summary>
        public RetryScheduler BaseScheduler { get; }

        /// <summary>
        /// Gets the maximum duration to attempt retries.
        /// </summary>
        public TimeSpan Timeout { get; }

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
        public override async Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            var maxDelay = Timeout - Elapsed;
            if (maxDelay <= TimeSpan.Zero)
                return false;

            using var ctsTimeout = new CancellationTokenSource(maxDelay);
            using var ctsCombined = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeout.Token, cancellationToken);
            try
            {
                return await BaseScheduler.WaitAsync(ctsCombined.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (ctsTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                return true; // Give it the last chance to retry.
            }
        }

        /// <summary>
        /// Resets the internal state of the scheduler to its initial condition.
        /// </summary>
        public override void Reset() => BaseScheduler.Reset();
    }
}
