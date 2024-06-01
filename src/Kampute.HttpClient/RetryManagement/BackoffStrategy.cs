// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryManagement
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// A factory for creating <see cref="RetryScheduler"/> instances configured with a specific retry strategy.
    /// </summary>
    /// <remarks>
    /// This factory encapsulates the creation logic of retry schedulers, allowing for consistent configuration of schedulers
    /// across an application. It uses a designated retry strategy to configure each scheduler it creates, ensuring that all
    /// schedulers have a uniform approach to handling retry attempts.
    /// </remarks>
    public class BackoffStrategy : IHttpBackoffProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackoffStrategy"/> class with a specified retry strategy.
        /// </summary>
        /// <param name="strategy">The retry strategy to be used by schedulers created by this factory.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategy"/> is <see langword="null"/>.</exception>
        public BackoffStrategy(IRetryStrategy strategy)
        {
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>
        /// Gets the retry strategy associated with this scheduler factory.
        /// </summary>
        public virtual IRetryStrategy Strategy { get; }

        /// <summary>
        /// Creates a <see cref="RetryScheduler"/> instance using the associated retry strategy.
        /// </summary>
        /// <returns>A new instance of <see cref="RetryScheduler"/> configured with the factory's retry strategy.</returns>
        public virtual IRetryScheduler CreateScheduler() => new RetryScheduler(Strategy);

        /// <inheritdoc/>
        IRetryScheduler IHttpBackoffProvider.CreateScheduler(HttpRequestErrorContext ctx) => CreateScheduler();
    }
}
