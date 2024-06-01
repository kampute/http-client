namespace Kampute.HttpClient.RetryManagement.Strategies.Modifiers
{
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// A retry strategy that limits the retry attempts to a specified time frame.
    /// </summary>
    /// <remarks>
    /// This class wraps another retry strategy and enforces a total time limit for retries. It ensures that the total elapsed time does
    /// not exceed the specified timeout before suggesting another retry attempt.
    /// </remarks>
    public sealed class LimitedDurationStrategyModifier : IRetryStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LimitedDurationStrategyModifier"/> class with a specified source retry strategy and timeout duration.
        /// </summary>
        /// <param name="source">The underlying retry strategy.</param>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public LimitedDurationStrategyModifier(IRetryStrategy source, TimeSpan timeout)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Timeout = timeout;
        }

        /// <summary>
        /// Gets the underlying retry strategy, to which the specified timeout duration is applied as a limit for the total retry attempts.
        /// </summary>
        /// <value>
        /// The underlying <see cref="IRetryStrategy"/>, to which the specified timeout duration is applied as a limit for the total retry attempts.
        /// </value>
        public IRetryStrategy Source { get; }

        /// <summary>
        /// Gets the maximum duration to continue attempting retries before giving up.
        /// </summary>
        /// <value>
        /// The maximum duration to continue attempting retries before giving up.
        /// </value>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Calculates the delay for the next retry attempt, enforcing the timeout limit.
        /// </summary>
        /// <param name="elapsed">The total time elapsed since the start of retry attempts.</param>
        /// <param name="attempts">The number of retry attempts made so far.</param>
        /// <param name="delay">When this method returns, contains the calculated delay for the next retry attempt. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the total elapsed time is within the specified timeout and the underlying strategy indicates that a retry should be attempted; otherwise, <see langword="false"/>.</returns>
        public bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay)
        {
            var remaining = Timeout - elapsed;
            if (remaining > TimeSpan.Zero && Source.TryGetRetryDelay(elapsed, attempts, out delay))
            {
                if (delay > remaining)
                    delay = remaining;

                return true;
            }

            delay = default;
            return false;
        }
    }
}
