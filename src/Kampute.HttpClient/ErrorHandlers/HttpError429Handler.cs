// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers.Abstracts;
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Net;

    /// <summary>
    /// Handles '429 Too Many Requests' HTTP responses by attempting to back off and retry the request according to a specified or
    /// default backoff strategy.
    /// </summary>
    /// <remarks>
    /// This handler provides a mechanism to respond to HTTP 429 errors by retrying the request after a delay. The delay duration and
    /// retry logic can be customized through the <see cref="RetryableHttpErrorHandler.OnBackoffStrategy"/> delegate. If the delegate
    /// is not provided, or does not specify a strategy, the handler will look for a rate limit reset header in the response. If the
    /// header is present, its value is used to determine the backoff duration. If the header is not present, no retries will be attempted.
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public class HttpError429Handler : RetryableHttpErrorHandler
    {
        /// <inheritdoc/>
        /// <remarks>
        /// This implementation specifically handles the HTTP '429 Too Many Requests' status code.
        /// </remarks>
        public sealed override bool CanHandle(HttpStatusCode statusCode) =>
#if NETSTANDARD2_1_OR_GREATER
            statusCode == HttpStatusCode.TooManyRequests;
#else
            statusCode == (HttpStatusCode)429;
#endif

        /// <inheritdoc/>
        protected override DateTimeOffset? GetSuggestedRetryTime(HttpResponseErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            ctx.Response.Headers.TryExtractRateLimitResetTime(out var resetTime);
            return resetTime;
        }

        /// <inheritdoc/>
        protected override IHttpBackoffProvider GetDefaultStrategy(HttpResponseErrorContext ctx, DateTimeOffset? retryTime)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            return retryTime.HasValue ? BackoffStrategies.Once(retryTime.Value) : BackoffStrategies.None;
        }
    }
}
