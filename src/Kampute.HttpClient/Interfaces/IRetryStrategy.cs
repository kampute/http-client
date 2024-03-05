// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using System;

    /// <summary>
    /// Defines a contract for a retry strategy that determines how HTTP request retries should be handled.
    /// </summary>
    /// <remarks>
    /// The <see cref="IRetryStrategy"/> interface facilitates the creation of various retry strategies, including but not limited to 
    /// backoff algorithms such as fixed delay, exponential backoff, or more sophisticated adaptive strategies. These strategies are 
    /// responsible for determining not only the delay between retry attempts but also whether and under what conditions a retry should 
    /// be attempted.
    /// </remarks>
    public interface IRetryStrategy
    {
        /// <summary>
        /// Creates a scheduler responsible for scheduling retry attempts for HTTP requests according to the strategy.
        /// </summary>
        /// <param name="ctx">The context containing detailed information about the failed HTTP request, including the client, request, and error information.</param>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that will manage the retry attempts for the given context in accordance with this strategy.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <c>null</c>.</exception>
        IRetryScheduler CreateScheduler(HttpRequestErrorContext ctx);
    }
}
