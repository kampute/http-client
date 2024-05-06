// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles '503 Service Unavailable' HTTP responses by attempting to back off and retry the request according to a specified or 
    /// default backoff strategy.
    /// </summary>
    /// <remarks>
    /// This handler provides a mechanism to respond to HTTP 503 errors by retrying the request after a delay. The delay duration and 
    /// retry logic can be customized through the <see cref="OnBackoffStrategy"/> delegate. If the delegate is not provided, or does not 
    /// specify a strategy, the handler will look for a <c>Retry-After</c> header in the response. If the <c>Retry-After</c> header is 
    /// present, its value is used to determine the backoff duration. If the header is not present, the default backoff strategy of the 
    /// <see cref="HttpRestClient"/> is used.
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    /// <seealso cref="HttpRestClient.BackoffStrategy"/>
    public class HttpError503Handler : IHttpErrorHandler
    {
        /// <summary>
        /// A delegate that allows customization of the backoff strategy when a '503 Service Unavailable' response is received.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this delegate is set and returns an <see cref="IHttpBackoffProvider"/>, the returned strategy is used for the retry operation. 
        /// If it is not set, or returns <c>null</c>, the handler will defer to the <c>Retry-After</c> header in the response or the 
        /// client's default backoff strategy.
        /// </para>
        /// <para>
        /// The delegate receives the following parameters:
        /// <list type="bullet">
        ///   <item>
        ///     <term>context</term>
        ///     <description>
        ///     Provides context about the HTTP responce indicating a '503 Service Unavailable' error. It is encapsulated within
        ///     an <see cref="HttpResponseErrorContext"/> instance, allowing for an informed decision on the retry strategy.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>retryAfter</term>
        ///     <description>
        ///       Advises on the next retry attempt timing as a <see cref="DateTimeOffset"/> value. If the response includes a <c>Retry-After</c>
        ///       header, this parameter reflects its value, suggesting an optimal time to retry. If the header is missing, the value is <c>null</c>,
        ///       indicating no specific suggestion from the server.
        ///     </description>
        ///   </item>
        /// </list>
        /// </para>
        /// </remarks>
        public Func<HttpResponseErrorContext, DateTimeOffset?, IHttpBackoffProvider?>? OnBackoffStrategy { get; set; }

        /// <summary>
        /// Determines whether this handler can process the specified HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns><c>true</c> if the handler can process the status code; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This implementation specifically handles the HTTP '503 Service Unavailable' status code.
        /// </remarks>
        public virtual bool CanHandle(HttpStatusCode statusCode) => statusCode == HttpStatusCode.ServiceUnavailable;

        /// <summary>
        /// Creates a scheduler for retrying the failed request based on the error context.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <returns>An <see cref="IRetryScheduler"/> that schedules the retry attempts.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method first attempts to use the <see cref="OnBackoffStrategy"/> delegate to obtain a retry strategy. If the delegate is not 
        /// provided or returns <c>null</c>, and a <c>Retry-After</c> header is present, the value of this header is used to create a retry 
        /// delay. If neither condition is met, the client's default backoff strategy is utilized.
        /// </remarks>
        protected virtual IRetryScheduler? CreateScheduler(HttpResponseErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            ctx.Response.Headers.TryExtractRetryAfterTime(out var retryAfter);

            var strategy = OnBackoffStrategy?.Invoke(ctx, retryAfter) ?? (retryAfter.HasValue ? BackoffStrategies.Once(retryAfter.Value) : ctx.Client.BackoffStrategy);
            return strategy.CreateScheduler(ctx);
        }

        /// <inheritdoc/>
        Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            return ctx.ScheduleRetryAsync(CreateScheduler, cancellationToken);
        }
    }
}
