// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Holds the retry budgets of one call to an <see cref="HttpRestClient"/>, shared by every attempt of that call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="HttpRestClient"/> creates one instance for each call it sends, and passes it to every error context created for that call.
    /// Each component that handles a kind of failure, such as the client for connection failures or an <see cref="IHttpErrorHandler"/> for error
    /// responses, has its own budget in this state. Because the state belongs to the call rather than to a request, the budgets are kept even when
    /// an error handler retries with a request it built itself instead of a clone of the failed request.
    /// </para>
    /// <para>
    /// Code that creates an <see cref="HttpRequestErrorContext"/> or <see cref="HttpResponseErrorContext"/> outside the client, such as a unit test
    /// of a custom error handler, creates a new instance for each call it simulates. An instance is not thread-safe, and must not be shared by calls
    /// that run concurrently.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRequestErrorContext.RetryState"/>
    public sealed class HttpRetryState
    {
        private readonly Dictionary<object, IRetryScheduler?> _schedulers = [];

        /// <summary>
        /// Returns the retry scheduler of the specified source, creating it on first use.
        /// </summary>
        /// <param name="source">The component that owns the retry budget.</param>
        /// <param name="schedulerFactory">The function that creates the scheduler, or returns <see langword="null"/> if the source does not retry.</param>
        /// <returns>The scheduler of <paramref name="source"/>, or <see langword="null"/> if the source does not retry.</returns>
        internal IRetryScheduler? GetOrCreateScheduler(object source, Func<IRetryScheduler?> schedulerFactory)
        {
            if (!_schedulers.TryGetValue(source, out var scheduler))
            {
                scheduler = schedulerFactory();
                _schedulers[source] = scheduler;
            }

            return scheduler;
        }
    }
}
