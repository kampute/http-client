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
    /// A scheduler that introduces jitter (randomness) to the delay between retry attempts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Jitter is introduced to the retry delays to prevent synchronization issues, such as the thundering herd problem, where 
    /// many clients might retry requests at the same time. Applying jitter spreads out the retry attempts over time. 
    /// </para>
    /// <para>
    /// The jitter factor allows fine-tuning of the randomness applied to the retry delay, enabling a balance between predictability
    /// and the benefits of desynchronization. It is a double value between 0 and 1 that determines the maximum proportion of the delay 
    /// that can be adjusted randomly to introduce jitter. A value of 0 means no jitter, while 1 allows the delay to vary by up to ±100% 
    /// of the base delay.
    /// </para>
    /// </remarks>
    public sealed class JitterRetryScheduler : RetryScheduler
    {
        private readonly Random _random = new();
        private double _currentJitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="JitterRetryScheduler"/> class with the specified underlying scheduler and jitter factor.
        /// </summary>
        /// <param name="scheduler">The base retry scheduler to which jitter will be added.</param>
        /// <param name="jitterFactor">The factor to apply to the delay to introduce jitter, must be between 0 and 1.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scheduler"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="jitterFactor"/> is less than 0 or greater than 1.</exception>
        public JitterRetryScheduler(RetryScheduler scheduler, double jitterFactor)
        {
            if (jitterFactor < 0.0 || jitterFactor > 1.0)
                throw new ArgumentOutOfRangeException(nameof(jitterFactor), "Jitter factor must be a value between 0 and 1.");

            BaseScheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            JitterFactor = jitterFactor;
            RecalculateJitter();
        }

        /// <summary>
        /// Gets the base retry scheduler to which jitter is added.
        /// </summary>
        public RetryScheduler BaseScheduler { get; }

        /// <summary>
        /// Gets the factor to apply to the delay to introduce jitter.
        /// </summary>
        /// <value>A floating-point number between 0 and 1, inclusive.</value>
        public double JitterFactor { get; }

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
        public override TimeSpan Delay => TimeSpan.FromMilliseconds((1 + _currentJitter) * BaseScheduler.Delay.TotalMilliseconds);

        /// <summary>
        /// Waits for the appropriate time before the next retry attempt, and determines if a retry should be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        /// <returns>A task that resolves to <c>true</c> if a retry should be attempted; otherwise, <c>false</c>.</returns>
        /// <exception cref="TaskCanceledException">Thrown if the wait operation is canceled.</exception>
        public override async Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            if (await BaseScheduler.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                RecalculateJitter();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the internal state of the scheduler to its initial condition.
        /// </summary>
        public override void Reset()
        {
            BaseScheduler.Reset();
            RecalculateJitter();
        }

        /// <summary>
        /// Recalculates the jitter value to be applied to the next delay.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecalculateJitter() => _currentJitter = JitterFactor * (2 * _random.NextDouble() - 1);
    }
}
