// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using Kampute.Resilience;
    using System;

    /// <summary>
    /// Defines how failed HTTP requests are retried.
    /// </summary>
    /// <remarks>
    /// A policy creates a retry session for a call when its request first fails in a way the policy covers. The session decides, for that failure
    /// and the later ones of the same kind during the call, whether and when the request is retried. <see cref="HttpRetryPolicy"/> creates sessions from an
    /// <see cref="IRetryStrategy"/>, and <see cref="HttpRetryPolicy.Dynamic(Func{HttpRequestErrorContext, IRetryStrategy})"/> chooses the strategy or the
    /// session from the failure.
    /// </remarks>
    /// <seealso cref="HttpRestClient.RetryPolicy"/>
    public interface IHttpRetryPolicy
    {
        /// <summary>
        /// Creates the retry session for a failed HTTP request.
        /// </summary>
        /// <param name="ctx">The context of the failure, with the client, the request and the error.</param>
        /// <returns>The <see cref="IRetrySession"/> that decides whether and when the request is retried.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        IRetrySession CreateSession(HttpRequestErrorContext ctx);
    }
}
