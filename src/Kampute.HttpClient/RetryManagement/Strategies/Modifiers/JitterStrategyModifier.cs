namespace Kampute.HttpClient.RetryManagement.Strategies.Modifiers
{
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// A retry strategy that adds random jitter to the delay durations provided by another retry strategy.
    /// </summary>
    /// <remarks>
    /// Jitter is added to the delays to prevent thundering herd problems and to provide a more distributed set of retry attempts over time. 
    /// This can be beneficial in high-load scenarios where many clients are retrying operations simultaneously.
    /// </remarks>
    public sealed class JitterStrategyModifier : IRetryStrategy
    {
        private readonly Random _random = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="JitterStrategyModifier"/> class with a specified source retry strategy and jitter factor.
        /// </summary>
        /// <param name="source">The underlying retry strategy to which jitter will be added.</param>
        /// <param name="jitterFactor">The factor to apply to the delay to introduce jitter, represented as a value between 0 and 1.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="jitterFactor"/> is not between 0 and 1.</exception>
        /// <remarks>
        /// The jitter factor allows fine-tuning of the randomness applied to the retry delay, enabling a balance between predictability and the
        /// benefits of desynchronization. It is a double value between 0 and 1 that determines the maximum proportion of the delay that can be
        /// adjusted randomly to introduce jitter. A value of 0 means no jitter, while 1 allows the delay to vary by up to ±100% of the base delay.
        /// </remarks>
        public JitterStrategyModifier(IRetryStrategy source, double jitterFactor)
        {
            if (jitterFactor < 0.0 || jitterFactor > 1.0)
                throw new ArgumentOutOfRangeException(nameof(jitterFactor), "Jitter factor must be a value between 0 and 1, inclusive.");

            Source = source ?? throw new ArgumentNullException(nameof(source));
            JitterFactor = jitterFactor;
        }

        /// <summary>
        /// Gets the underlying retry strategy to which jitter is added.
        /// </summary>
        public IRetryStrategy Source { get; }

        /// <summary>
        /// Gets actor to apply to the delay to introduce jitter.
        /// </summary>
        /// <value>A floating-point number between 0 and 1, inclusive.</value>
        public double JitterFactor { get; }

        /// <summary>
        /// Calculates the delay for the next retry attempt, adding random jitter based on the jitter factor to the delay provided by the underlying strategy.
        /// </summary>
        /// <param name="elapsed">The total time elapsed since the start of retry attempts.</param>
        /// <param name="attempts">The number of retry attempts made so far.</param>
        /// <param name="delay">When this method returns, contains the calculated delay for the next retry attempt. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the underlying strategy indicates that a retry should be attempted; otherwise, <c>false</c>.</returns>
        public bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay)
        {
            if (Source.TryGetRetryDelay(elapsed, attempts, out delay))
            {
                var jitter = delay.TotalMilliseconds * JitterFactor * (2 * _random.NextDouble() - 1);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitter);
                return true;
            }
            return false;
        }
    }
}
