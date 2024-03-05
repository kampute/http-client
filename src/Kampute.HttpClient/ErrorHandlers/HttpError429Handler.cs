// Copyright (C) 2024 Kampute
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
    /// retry logic can be customized through the <see cref="OnBackoffStrategy"/> delegate. If the delegate is not provided, or does not 
    /// specify a strategy, the handler will look for a rate limit reset header in the response. If the header is present, its value is 
    /// used to determine the backoff duration. If the header is not present, no retries will be attempted.
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public class HttpError429Handler : HttpErrorHandlerWithBackoff
    {
        /// <summary>
        /// A delegate that allows customization of the backoff strategy when a 429 Too Many Requests' response is received.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this delegate is set and returns an <see cref="IRetryStrategy"/>, the returned strategy is used for the retry operation. 
        /// If it is not set, or returns <c>null</c>, the handler will defer to the <c>Retry-After</c> header in the response.
        /// </para>
        /// <para>
        /// The delegate receives the following parameters:
        /// <list type="bullet">
        /// <item>
        /// <term>context</term>
        /// <description>Provides context about the HTTP request resulting in a '429 Too Many Requests' response. It is encapsulated 
        /// within an <see cref="HttpResponseErrorContext"/> instance, allowing for an informed decision on the retry strategy.</description>
        /// </item>
        /// <item>
        /// <term>resetTime</term>
        /// <description>Indicates the time when the rate limit will be lifted as a <see cref="DateTimeOffset"/> value. If the server specifies 
        /// a reset time via response headers, this parameter provides that time, allowing the client to know when to resume requests. If the 
        /// server does not specify a reset time, the value will be <c>null</c>.</description>
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        public Func<HttpResponseErrorContext, DateTimeOffset?, IRetryStrategy?>? OnBackoffStrategy { get; set; }

        /// <summary>
        /// Determines whether this handler can process the specified HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns><c>true</c> if the handler can process the status code; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This implementation specifically handles the HTTP '429 Too Many Requests' status code.
        /// </remarks>
        public override bool CanHandle(HttpStatusCode statusCode) =>
#if NETSTANDARD2_1_OR_GREATER
            statusCode == HttpStatusCode.TooManyRequests;
#else
            statusCode == (HttpStatusCode)429;
#endif

        /// <summary>
        /// Determines the backoff strategy to use based on the error context.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <returns>An <see cref="IRetryStrategy"/> that defines the backoff strategy.</returns>
        /// <remarks>
        /// This method first attempts to use the <see cref="OnBackoffStrategy"/> delegate to obtain a retry strategy. If the delegate is not 
        /// provided or returns <c>null</c>, and a rate limit reset header is present, the value of this header is used to create a retry delay. 
        /// If neither condition is met, no retries will be attempted.
        /// </remarks>
        protected override IRetryStrategy DetermineBackoffPolicy(HttpResponseErrorContext ctx)
        {
            ctx.Response.Headers.TryExtractRateLimitResetTime(out var resetTime);

            var strategy = OnBackoffStrategy?.Invoke(ctx, resetTime);
            if (strategy is not null)
                return strategy;

            return resetTime.HasValue ? BackoffStrategies.Once(resetTime.Value) : BackoffStrategies.None;
        }
    }
}