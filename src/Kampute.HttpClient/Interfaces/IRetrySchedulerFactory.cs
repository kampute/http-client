// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using System;

    /// <summary>
    /// Defines a contract for creating retry schedulers tailored to specific retry strategies for HTTP requests.
    /// </summary>
    /// <remarks>
    /// The <see cref="IRetrySchedulerFactory"/> interface enables the implementation of various retry strategies, such as fixed delay, exponential backoff, 
    /// or adaptive strategies, for handling HTTP request retries. It focuses on generating schedulers that decide the timing and conditions under which retries occur.
    /// </remarks>
    public interface IRetrySchedulerFactory
    {
        /// <summary>
        /// Creates a scheduler responsible for scheduling retry attempts for HTTP requests according to the strategy.
        /// </summary>
        /// <param name="ctx">The context containing detailed information about the failed HTTP request, including the client, request, and error information.</param>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that will manage the retry attempts for the given context in accordance with the strategy provided by this factory.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <c>null</c>.</exception>
        IRetryScheduler CreateScheduler(HttpRequestErrorContext ctx);
    }
}
