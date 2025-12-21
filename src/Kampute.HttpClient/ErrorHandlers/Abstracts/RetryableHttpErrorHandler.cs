// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers.Abstracts
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides the base functionality for handling HTTP responses with transient error status codes by attempting to back off and
    /// retry the request according to a specified or default backoff strategy.
    /// </summary>
    /// <remarks>
    /// This handler class is designed to be extended for specific transient error status codes. It offers a mechanism to respond to
    /// transient HTTP errors by retrying the request after a delay. The delay duration and retry logic can be customized through the
    /// <see cref="OnBackoffStrategy"/> delegate.
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    /// <seealso cref="HttpRestClient.BackoffStrategy"/>
    public abstract class RetryableHttpErrorHandler : IHttpErrorHandler
    {
        /// <summary>
        /// A delegate that allows customization of the backoff strategy when responses with transient error status codes are received.
        /// </summary>
        /// <value>
        /// A function that takes an <see cref="HttpResponseErrorContext"/> and an optional <see cref="DateTimeOffset"/> representing
        /// the suggested retry time, and returns an <see cref="IHttpBackoffProvider"/> to be used for the retry operation.
        /// </value>
        /// <remarks>
        /// <para>
        /// If this delegate is set and returns an <see cref="IHttpBackoffProvider"/>, the returned strategy is used for the retry operation.
        /// If it is not set, or returns <see langword="null"/>, a default behavior is applied.
        /// </para>
        /// <para>
        /// The delegate receives the following parameters:
        /// <list type="bullet">
        ///   <item>
        ///     <term>context</term>
        ///     <description>
        ///     Provides context about the HTTP response that indicates a transient error. It is encapsulated within an <see cref="HttpResponseErrorContext"/>
        ///     instance, allowing for an informed decision on the retry strategy.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>retryTime</term>
        ///     <description>
        ///       Advises on the next retry attempt timing as a <see cref="DateTimeOffset"/> value if the response suggests one. If the response
        ///       does not include a suggested retry time, the value will be <see langword="null"/>.
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
        /// <returns><see langword="true"/> if the handler can process the status code; otherwise, <see langword="false"/>.</returns>
        public abstract bool CanHandle(HttpStatusCode statusCode);

        /// <summary>
        /// Extracts the suggested retry time from the HTTP response's header, if present.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response.</param>
        /// <returns>The suggested <see cref="DateTimeOffset"/> to retry the request, or <see langword="null"/> if the header is not present.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        protected virtual DateTimeOffset? GetSuggestedRetryTime(HttpResponseErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            ctx.Response.Headers.TryExtractRetryAfterTime(out var retryTime);
            return retryTime;
        }

        /// <summary>
        /// Provides the default backoff strategy when no custom strategy is specified.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response.</param>
        /// <param name="retryTime">The suggested retry time, if any.</param>
        /// <returns>An <see cref="IHttpBackoffProvider"/> representing the default backoff strategy.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        protected virtual IHttpBackoffProvider GetDefaultStrategy(HttpResponseErrorContext ctx, DateTimeOffset? retryTime)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            return retryTime.HasValue ? BackoffStrategies.Once(retryTime.Value) : ctx.Client.BackoffStrategy;
        }

        /// <summary>
        /// Creates a scheduler for retrying the failed request based on the error context.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <returns>An <see cref="IRetryScheduler"/> that schedules the retry attempts.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The method uses <see cref="OnBackoffStrategy"/> when available. If the delegate is not provided or returns
        /// <see langword="null"/>, and the response includes a suggested retry time, a single retry at that time is used.
        /// Otherwise the client's default backoff strategy is used.
        /// </remarks>
        protected virtual IRetryScheduler? CreateScheduler(HttpResponseErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            var retryTime = GetSuggestedRetryTime(ctx);
            var strategy = OnBackoffStrategy?.Invoke(ctx, retryTime) ?? GetDefaultStrategy(ctx, retryTime);
            return strategy.CreateScheduler(ctx);
        }

        /// <inheritdoc/>
        Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            return ctx.ScheduleRetryAsync(CreateScheduler, cancellationToken);
        }
    }
}
