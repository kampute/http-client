namespace Kampute.HttpClient.RetryManagement.Strategies.Modifiers
{
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// A retry strategy that limits the number of retry attempts to a specified maximum.
    /// </summary>
    /// <remarks>
    /// This class wraps another strategy and enforces a maximum number of attempts. Once the limit is reached, no further
    /// retries are suggested.
    /// </remarks>
    public sealed class LimitedAttemptsStrategyModifier : IRetryStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LimitedAttemptsStrategyModifier"/> class with a specified source provider and maximum number of
        /// attempts.
        /// </summary>
        /// <param name="source">The underlying retry strategy.</param>
        /// <param name="maxAttempts">The maximum number of retry attempts before giving up.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <c>null</c>.</exception>
        public LimitedAttemptsStrategyModifier(IRetryStrategy source, uint maxAttempts)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            MaxAttempts = maxAttempts;
        }

        /// <summary>
        /// Gets the underlying retry strategy, to which the specified maximum number of retry attempts is applied as a limit.
        /// </summary>
        /// <value>
        /// The underlying <see cref="IRetryStrategy"/>, to which the specified maximum number of retry attempts is applied as a limit.
        /// </value>
        public IRetryStrategy Source { get; }

        /// <summary>
        /// Gets the maximum number of retry attempts before giving up.
        /// </summary>
        /// <value>
        /// The maximum number of retry attempts before giving up.
        /// </value>
        public uint MaxAttempts { get; }

        /// <summary>
        /// Calculates the delay for the next retry attempt, enforcing the maximum attempt limit.
        /// </summary>
        /// <param name="elapsed">The total time elapsed since the start of retry attempts.</param>
        /// <param name="attempts">The number of retry attempts made so far.</param>
        /// <param name="delay">When this method returns, contains the calculated delay for the next retry attempt. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the number of attempts has not yet reached the maximum and the underlying strategy indicates that a retry should be attempted; otherwise, <c>false</c>.</returns>
        public bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay)
        {
            if (attempts < MaxAttempts & Source.TryGetRetryDelay(elapsed, attempts, out delay))
                return true;

            delay = default;
            return false;
        }
    }
}
