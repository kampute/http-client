// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers.Abstracts;
    using System.Net;

    /// <summary>
    /// Handles '503 Service Unavailable' responses by retrying the request after a delay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the first '503 Service Unavailable' response of a call has a <c>Retry-After</c> header, the request is retried once, at the time the
    /// header suggests; a repeated 503 response then reaches the caller. Without the header, the request is retried as the
    /// <see cref="HttpRestClient.RetryPolicy"/> of the client decides. <see cref="RetryableHttpErrorHandler.OnRetryPolicy"/> can choose another policy.
    /// If a suggested time is further away than <see cref="RetryableHttpErrorHandler.MaxRetryDelay"/>, which is five minutes by default, the request is
    /// not retried. See <see cref="RetryableHttpErrorHandler"/> for how the retries of a call are decided.
    /// </para>
    /// <note type="hint" title="Hint">
    /// Consider using <see cref="TransientHttpErrorHandler"/> if you want to handle multiple transient HTTP errors (including 503) with
    /// a single handler.
    /// </note>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    /// <seealso cref="HttpRestClient.RetryPolicy"/>
    /// <seealso cref="TransientHttpErrorHandler"/>
    public class HttpError503Handler : RetryableHttpErrorHandler
    {
        /// <inheritdoc/>
        /// <remarks>
        /// This handler handles the '503 Service Unavailable' status code only.
        /// </remarks>
        public sealed override bool CanHandle(HttpStatusCode statusCode) => statusCode == HttpStatusCode.ServiceUnavailable;
    }
}
