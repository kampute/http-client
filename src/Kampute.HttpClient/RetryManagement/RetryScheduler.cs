// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryManagement
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a scheduler for managing retry attempts.
    /// </summary>
    public class RetryScheduler : IRetryScheduler
    {
        private readonly Stopwatch _timer = Stopwatch.StartNew();
        private uint _attempts = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryScheduler"/> class with a specified retry strategy.
        /// </summary>
        /// <param name="strategy">The retry strategy to be used by this scheduler.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategy"/> is <c>null</c>.</exception>
        public RetryScheduler(IRetryStrategy strategy)
        {
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>
        /// Gets the retry strategy associated with this scheduler.
        /// </summary>
        public virtual IRetryStrategy Strategy { get; }

        /// <summary>
        /// Gets the number of retry attempts that have been made.
        /// </summary>
        public virtual uint Attempts => _attempts;

        /// <summary>
        /// Gets the total elapsed time since the retry attempts were started.
        /// </summary>
        public virtual TimeSpan Elapsed => _timer.Elapsed;

        /// <summary>
        /// Waits for the appropriate time before the next retry attempt, and determines if a retry should be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        /// <returns>A task that resolves to <c>true</c> if a retry should be attempted; otherwise, <c>false</c>.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the wait operation is canceled.</exception>
        public virtual async Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            if (Strategy.TryGetRetryDelay(Elapsed, Attempts, out var delay))
            {
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                else
                    cancellationToken.ThrowIfCancellationRequested();

                ReadyNextAttempt();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the internal state of the scheduler to its initial condition.
        /// </summary>
        public virtual void Reset()
        {
            _timer.Restart();
            _attempts = 0;
        }

        /// <summary>
        /// Prepares the internal state for the next retry attempt.
        /// </summary>
        /// <remarks>
        /// This method is called immediately after a retry attempt is determined to be necessary and before the delay for the next attempt begins. 
        /// It allows for updating the internal state or performing any preparations required before the next attempt.
        /// </remarks>
        protected virtual void ReadyNextAttempt()
        {
            ++_attempts;
        }
    }
}
