// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers.Abstracts;
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A scheduler that manages retry attempts with an increased (or decreased) delay based on the Fibonacci sequence.
    /// </summary>
    /// <remarks>
    /// The <see cref="FibonacciRetryScheduler"/> class uses the Fibonacci sequence to adjust the wait time between retries, starting 
    /// with an initial delay and scaling the delay between subsequent attempts according to Fibonacci numbers. This approach provides 
    /// a more moderate and controlled increase in delay times compared to <see cref="ExponentialRetryScheduler"/>, making it suitable 
    /// for scenarios where a less aggressive increase in delay is desired.
    /// </remarks>
    public sealed class FibonacciRetryScheduler : RetryScheduler
    {
        private int _previousFib = 1;
        private int _currentFib = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="FibonacciRetryScheduler"/> class.
        /// </summary>
        /// <param name="initialDelay">The initial delay duration before the first retry attempt.</param>
        public FibonacciRetryScheduler(TimeSpan initialDelay)
        {
            InitialDelay = initialDelay;
        }

        /// <summary>
        /// Gets the initial delay duration before the first retry attempt.
        /// </summary>
        public TimeSpan InitialDelay { get; }

        /// <summary>
        /// Gets the delay duration before the next attempt.
        /// </summary>
        public override TimeSpan Delay => TimeSpan.FromTicks(InitialDelay.Ticks * _currentFib);

        /// <summary>
        /// Waits for the appropriate time before the next retry attempt, and determines if a retry should be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        /// <returns>A task that resolves to <c>true</c> if a retry should be attempted; otherwise, <c>false</c>.</returns>
        /// <exception cref="TaskCanceledException">Thrown if the wait operation is canceled.</exception>
        public override async Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            if (await base.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                UpdateFibonacciNumber();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the internal state of the scheduler to its initial condition.
        /// </summary>
        public override void Reset()
        {
            _previousFib = 1;
            _currentFib = 1;
            base.Reset();
        }

        /// <summary>
        /// Updates the Fibonacci numbers used to calculate the delay before the next retry attempt.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateFibonacciNumber()
        {
            var nextFib = _previousFib + _currentFib;
            _previousFib = _currentFib;
            _currentFib = nextFib;
        }
    }
}
