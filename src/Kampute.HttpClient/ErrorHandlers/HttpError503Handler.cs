// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers.Abstracts;
    using System.Net;

    /// <summary>
    /// Handles '503 Service Unavailable' HTTP responses by attempting to back off and retry the request according to a specified or
    /// default retry policy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler provides a mechanism to respond to HTTP 503 errors by retrying the request after a delay. The delay duration and
    /// retry logic can be customized through the <see cref="RetryableHttpErrorHandler.OnRetryPolicy"/> delegate. If the delegate
    /// is not provided, or does not specify a strategy, the handler will look for a <c>Retry-After</c> header in the response. If the
    /// <c>Retry-After</c> header is present, its value is used to determine the delay before the retry. If the header is not present, the
    /// retry policy of the <see cref="HttpRestClient"/> is used. If the <c>Retry-After</c> time is further away than
    /// <see cref="RetryableHttpErrorHandler.MaxRetryDelay"/>, which is five minutes by default, the request is not retried.
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
        /// This implementation specifically handles the HTTP '503 Service Unavailable' status code.
        /// </remarks>
        public sealed override bool CanHandle(HttpStatusCode statusCode) => statusCode == HttpStatusCode.ServiceUnavailable;
    }
}
