// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryStrategies.Abstracts
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetrySchedulers;
    using Kampute.HttpClient.RetrySchedulers.Abstracts;
    using System;

    /// <summary>
    /// Represents an abstract base class for defining strategies used in retry logic.
    /// </summary>
    /// <remarks>
    /// This class provides a framework for implementing different types of backoff strategies (e.g., linear, exponential)
    /// that determine how long the system should wait before retrying an operation after a failure.
    /// </remarks>
    public abstract class RetryStrategy : IRetryStrategy
    {
        private double _jitterFactor = 0.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryStrategy"/> class without constraints.
        /// </summary>
        protected RetryStrategy() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryStrategy"/> class with a specified maximum number of retry attempts.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts before giving up.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAttempts"/> is less than 1.</exception>
        protected RetryStrategy(int maxAttempts)
        {
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1.");

            MaxAttempts = maxAttempts;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryStrategy"/> class with a specified timeout duration for retries.
        /// </summary>
        /// <param name="timeout">The maximum duration to continue attempting retries before giving up.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not a positive duration.</exception>
        protected RetryStrategy(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be a positive duration.");

            Timeout = timeout;
        }

        /// <summary>
        /// Gets the maximum number of retry attempts or <c>null</c> if there is no maximum attempts constraint.
        /// </summary>
        public int? MaxAttempts { get; }

        /// <summary>
        /// Gets the maximum duration to attempt retries or <c>null</c> if there is no timeout constraint.
        /// </summary>
        public TimeSpan? Timeout { get; }

        /// <summary>
        /// Gets or sets the factor to apply to the delay to introduce jitter.
        /// </summary>
        /// <value>A floating-point number between 0 and 1, inclusive.</value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the set value is less than 0 or greater than 1.</exception>
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
        public double JitterFactor
        {
            get => _jitterFactor;
            set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(JitterFactor), "Jitter factor must be a value between 0 and 1.");

                _jitterFactor = value;
            }
        }

        /// <summary>
        /// Creates a scheduler responsible for scheduling retry attempts according to the strategy.
        /// </summary>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that schedules retries with a linearly adjusted delay.</returns>
        public virtual RetryScheduler CreateScheduler()
        {
            var scheduler = CreateBaseScheduler();

            if (JitterFactor != 0.0)
                scheduler = new JitterRetryScheduler(scheduler, JitterFactor);
            if (MaxAttempts.HasValue)
                scheduler = new AttemptLimitRetryScheduler(scheduler, MaxAttempts.Value);
            if (Timeout.HasValue)
                scheduler = new TimeLimitRetryScheduler(scheduler, Timeout.Value);

            return scheduler;
        }

        /// <summary>
        /// Creates a scheduler without constraints responsible for scheduling retry attempts according to the strategy.
        /// </summary>
        /// <remarks>
        /// This method provides a scheduler that implements the strategy specified by the <see cref="RetryStrategy"/> instance for retrying 
        /// operations, without imposing any limits on the number of attempts or the overall timeout. It forms the basis for potentially adding 
        /// constraints, such as attempt limits or timeouts, through further composition to tailor the retry behavior to specific requirements.
        /// </remarks>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that applies the backoff strategy in an unconstrained manner.</returns>
        protected abstract RetryScheduler CreateBaseScheduler();

        /// <inheritdoc/>
        IRetryScheduler IRetryStrategy.CreateScheduler(HttpRequestErrorContext ctx) => CreateScheduler();
    }
}
