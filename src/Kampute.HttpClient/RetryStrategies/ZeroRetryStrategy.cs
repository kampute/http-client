// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryStrategies
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetrySchedulers;

    /// <summary>
    /// A backoff strategy that disables retries.
    /// </summary>
    /// <remarks>
    /// The <see cref="ZeroRetryStrategy"/> class implements the <see cref="IRetryStrategy"/> interface to provide a strategy where no retry 
    /// should be attempted.
    /// </remarks>
    public sealed class ZeroRetryStrategy : IRetryStrategy
    {
        private ZeroRetryStrategy() { }

        /// <summary>
        /// Gets the singleton instance of the <see cref="ZeroRetryStrategy"/> class.
        /// </summary>
        public static ZeroRetryStrategy Instance { get; } = new();

        /// <summary>
        /// Gets a scheduler that enforces no retries.
        /// </summary>
        public ZeroRetryScheduler Scheduler => ZeroRetryScheduler.Instance;

        /// <summary>
        /// Creates a scheduler that enforces no retries.
        /// </summary>
        /// <param name="ctx">The context containing detailed information about the failed HTTP request.</param>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that always indicates no retry should be attempted.</returns>
        IRetryScheduler IRetryStrategy.CreateScheduler(HttpRequestErrorContext ctx) => Scheduler;
    }
}
