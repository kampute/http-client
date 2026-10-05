// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers.Abstracts;
    using Kampute.HttpClient.Interfaces;
    using Kampute.Resilience;
    using System;
    using System.Net;

    /// <summary>
    /// Handles '429 Too Many Requests' responses by retrying the request when the server allows it.
    /// </summary>
    /// <remarks>
    /// If the first '429 Too Many Requests' response of a call suggests a retry time, in a <c>Retry-After</c> header or otherwise in a rate limit
    /// reset header such as <c>x-ratelimit-reset</c>, the request is retried once, at that time; a repeated 429 response then reaches the caller.
    /// Without a suggested time, the request is not retried. <see cref="RetryableHttpErrorHandler.OnRetryPolicy"/> can choose another policy. If a
    /// suggested time is further away than <see cref="RetryableHttpErrorHandler.MaxRetryDelay"/>, which is five minutes by default, the request is not
    /// retried. See <see cref="RetryableHttpErrorHandler"/> for how the retries of a call are decided, and
    /// <see cref="HttpResponseHeadersExtensions.TryExtractRateLimitResetTime"/> for the headers that are read.
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public class HttpError429Handler : RetryableHttpErrorHandler
    {
        /// <inheritdoc/>
        /// <remarks>
        /// This handler handles the '429 Too Many Requests' status code only.
        /// </remarks>
        public sealed override bool CanHandle(HttpStatusCode statusCode) =>
#if !NETSTANDARD2_0
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
        protected override IHttpRetryPolicy GetDefaultPolicy(HttpResponseErrorContext ctx, DateTimeOffset? retryTime)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            return retryTime.HasValue ? RetryStrategies.Once(retryTime.Value).ToHttpRetryPolicy() : HttpRetryPolicy.None;
        }
    }
}
