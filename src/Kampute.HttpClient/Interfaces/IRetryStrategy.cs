namespace Kampute.HttpClient.Interfaces
{
    using System;

    /// <summary>
    /// Defines a strategy for calculating the delay duration between retry attempts based on elapsed time and the number of attempts already made.
    /// </summary>
    public interface IRetryStrategy
    {
        /// <summary>
        /// Calculates the delay duration for the next retry attempt and indicates whether a retry should be attempted.
        /// </summary>
        /// <param name="elapsed">The total time elapsed since the start of retry attempts.</param>
        /// <param name="attempts">The count of retry attempts made so far.</param>
        /// <param name="delay">When this method returns, contains the calculated delay duration for the next retry attempt, if a retry is advisable. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if a retry attempt is advisable and should be made after the calculated delay; <see langword="false"/> otherwise, indicating no further retry attempts should be made.</returns>
        bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay);
    }
}
