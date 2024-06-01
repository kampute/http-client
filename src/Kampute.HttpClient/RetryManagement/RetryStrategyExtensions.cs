namespace Kampute.HttpClient.RetryManagement
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement.Strategies.Modifiers;
    using System;

    /// <summary>
    /// Provides extension methods for <see cref="IRetryStrategy"/> to enhance retry strategies with additional behavior.
    /// </summary>
    public static class RetryStrategyExtensions
    {
        /// <summary>
        /// Enhances a retry strategy with jitter to add randomness to the retry delay.
        /// </summary>
        /// <param name="source">The original retry strategy to be enhanced.</param>
        /// <param name="jitterFactor">The factor by which to adjust the delay randomly, with a default of 0.5.</param>
        /// <returns>A <see cref="Strategies.Modifiers.JitterStrategyModifier"/> instance wrapping the original retry strategy with added jitter.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="jitterFactor"/> is not between 0 and 1.</exception>
        public static JitterStrategyModifier WithJitter(this IRetryStrategy source, double jitterFactor = 0.5) => new(source, jitterFactor);

        /// <summary>
        /// Enhances a retry strategy with a maximum number of retry attempts.
        /// </summary>
        /// <param name="source">The original retry strategy to be enhanced.</param>
        /// <param name="maxAttempts">The maximum number of attempts allowed before giving up.</param>
        /// <returns>A <see cref="LimitedAttemptsStrategyModifier"/> instance wrapping the original retry strategy with a limit on the number of attempts.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static LimitedAttemptsStrategyModifier WithMaxAttempts(this IRetryStrategy source, uint maxAttempts) => new(source, maxAttempts);

        /// <summary>
        /// Enhances a retry strategy with a timeout, limiting the total duration allowed for retry attempts.
        /// </summary>
        /// <param name="source">The original retry strategy to be enhanced.</param>
        /// <param name="timeout">The maximum duration to attempt retries before giving up.</param>
        /// <returns>A <see cref="LimitedDurationStrategyModifier"/> instance wrapping the original retry strategy with a timeout limit.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static LimitedDurationStrategyModifier WithTimeout(this IRetryStrategy source, TimeSpan timeout) => new(source, timeout);

        /// <summary>
        /// Converts an <see cref="IRetryStrategy"/> into a <see cref="RetryScheduler"/>, creating a scheduler instance based on the provided strategy.
        /// </summary>
        /// <param name="source">The retry strategy to convert into a scheduler.</param>
        /// <returns>A new instance of <see cref="RetryScheduler"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static RetryScheduler ToScheduler(this IRetryStrategy source) => new(source);

        /// <summary>
        /// Converts an <see cref="IRetryStrategy"/> into a <see cref="BackoffStrategy"/>, creating a factory capable of producing schedulers based
        /// on the provided strategy.
        /// </summary>
        /// <param name="source">The retry strategy to use for creating a scheduler factory.</param>
        /// <returns>A new instance of <see cref="BackoffStrategy"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static BackoffStrategy ToBackoffStrategy(this IRetryStrategy source) => new(source);
    }
}
