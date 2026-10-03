// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.Retry package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.Retry
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the retry state of one operation that a retry strategy governs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The session counts the retry attempts and measures the time since it was created, and asks its <see cref="Strategy"/> for the delay before each
    /// retry. Use one session per operation; the strategy can be shared.
    /// </para>
    /// <para>
    /// The longest delay a session waits is <see cref="int.MaxValue"/> milliseconds (about 24.8 days), the limit of <see cref="Task.Delay(TimeSpan, CancellationToken)"/>
    /// on .NET Framework. If the strategy returns a longer delay, the session does not retry.
    /// </para>
    /// </remarks>
    public class RetrySession : IRetrySession
    {
        private readonly Stopwatch _timer = Stopwatch.StartNew();
        private uint _attempts = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetrySession"/> class with a specified retry strategy.
        /// </summary>
        /// <param name="strategy">The retry strategy that decides the delay before each retry.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategy"/> is <see langword="null"/>.</exception>
        public RetrySession(IRetryStrategy strategy)
        {
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>
        /// Gets the retry strategy of this session.
        /// </summary>
        /// <value>The <see cref="IRetryStrategy"/> that decides the delay before each retry.</value>
        public virtual IRetryStrategy Strategy { get; }

        /// <summary>
        /// Gets the number of retry attempts that have been made.
        /// </summary>
        /// <value>The number of retries this session has allowed.</value>
        public virtual uint Attempts => _attempts;

        /// <summary>
        /// Gets the time elapsed since the session was created or last reset.
        /// </summary>
        /// <value>The elapsed time as a <see cref="TimeSpan"/>.</value>
        public virtual TimeSpan Elapsed => _timer.Elapsed;

        /// <summary>
        /// Asynchronously waits for the delay that the strategy sets before the next retry attempt, and determines whether a retry should be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait.</param>
        /// <returns>A task that resolves to <see langword="true"/> if a retry should be attempted after the wait; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled.</exception>
        public virtual async Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            if (!TryBeginNextAttempt(out var delay))
                return false;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            else
                cancellationToken.ThrowIfCancellationRequested();

            return true;
        }

        /// <summary>
        /// Blocks the calling thread for the delay that the strategy sets before the next retry attempt, and determines whether a retry should be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait.</param>
        /// <returns><see langword="true"/> if a retry should be attempted after the wait; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled. A wait in progress ends as soon as the token is canceled.</exception>
        public virtual bool Wait(CancellationToken cancellationToken)
        {
            if (!TryBeginNextAttempt(out var delay))
                return false;

            if (delay > TimeSpan.Zero)
            {
                if (cancellationToken.CanBeCanceled)
                    cancellationToken.WaitHandle.WaitOne(delay);
                else
                    Thread.Sleep(delay);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        /// <summary>
        /// Resets the session to its initial state: no attempts, and the elapsed time restarted.
        /// </summary>
        public virtual void Reset()
        {
            _timer.Restart();
            _attempts = 0;
        }

        /// <summary>
        /// Updates the state of the session when a retry attempt is allowed.
        /// </summary>
        /// <remarks>
        /// This method is called when the strategy allows another attempt, before the wait for its delay begins. The base implementation counts the attempt.
        /// </remarks>
        protected virtual void ReadyNextAttempt()
        {
            ++_attempts;
        }

        /// <summary>
        /// Asks the strategy for the delay before the next attempt, applies the longest supported delay, and counts the attempt if it is allowed.
        /// </summary>
        /// <param name="delay">When this method returns <see langword="true"/>, the delay to wait before the next attempt.</param>
        /// <returns><see langword="true"/> if another attempt is allowed; otherwise, <see langword="false"/>.</returns>
        private bool TryBeginNextAttempt(out TimeSpan delay)
        {
            if (!Strategy.TryGetRetryDelay(Elapsed, Attempts, out delay) || delay.TotalMilliseconds > int.MaxValue)
                return false;

            ReadyNextAttempt();
            return true;
        }
    }
}
