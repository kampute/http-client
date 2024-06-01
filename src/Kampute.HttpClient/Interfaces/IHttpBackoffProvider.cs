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
    /// This interface allows for the implementation of various retry strategies tailored to HTTP communications, such as fixed delay, exponential backoff, 
    /// or adaptive strategies. It primarily focuses on generating schedulers that determine the timing and conditions for retry attempts based on the nature o
    /// f HTTP request failures.
    /// </remarks>
    public interface IHttpBackoffProvider
    {
        /// <summary>
        /// Creates a scheduler responsible for managing retry attempts for HTTP requests, based on a specified retry strategy.
        /// </summary>
        /// <param name="ctx">Provides context containing detailed information about the failed HTTP request, including client, request, and error specifics.</param>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that coordinates the retry attempts for the given context according to the defined strategy.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        IRetryScheduler CreateScheduler(HttpRequestErrorContext ctx);
    }
}
