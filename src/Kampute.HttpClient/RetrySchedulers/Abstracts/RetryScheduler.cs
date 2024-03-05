// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetrySchedulers.Abstracts
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an abstract scheduler for managing retry attempts.
    /// </summary>
    /// <remarks>
    /// This abstract class provides the common functionality for scheduling retries, including tracking the number of attempts 
    /// made and determining the delay before the next attempt.
    /// </remarks>
    public abstract class RetryScheduler : IRetryScheduler
    {
        private readonly Stopwatch _timer = Stopwatch.StartNew();
        private int _attempts = 0;

        /// <summary>
        /// Gets the number of retry attempts that have been made.
        /// </summary>
        public virtual int Attempts => _attempts;

        /// <summary>
        /// Gets the total elapsed time since the retry attempts was started.
        /// </summary>
        public virtual TimeSpan Elapsed => _timer.Elapsed;

        /// <summary>
        /// Gets the delay duration before the next attempt.
        /// </summary>
        public abstract TimeSpan Delay { get; }

        /// <summary>
        /// Waits for the appropriate time before the next retry attempt, and determines if a retry should be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        /// <returns>A task that always resolves to <c>true</c>.</returns>
        /// <exception cref="TaskCanceledException">Thrown if the wait operation is canceled.</exception>
        public virtual async Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMilliseconds(Math.Max(0, Delay.TotalMilliseconds));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

            _attempts++;
            return true;
        }

        /// <summary>
        /// Resets the internal state of the scheduler to its initial condition.
        /// </summary>
        public virtual void Reset()
        {
            _timer.Restart();
            _attempts = 0;
        }
    }
}
