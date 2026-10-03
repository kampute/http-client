// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers.Abstracts
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.Retry;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides the base functionality for handling HTTP responses with transient error status codes by attempting to back off and
    /// retry the request according to a specified or default backoff strategy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler class is designed to be extended for specific transient error status codes. It offers a mechanism to respond to
    /// transient HTTP errors by retrying the request after a delay. The delay duration and retry logic can be customized through the
    /// <see cref="OnBackoffStrategy"/> delegate.
    /// </para>
    /// <para>
    /// A retry time suggested by the server is honored only if it is no further away than <see cref="MaxRetryDelay"/>, which is five minutes
    /// by default. If the suggested time is later, the request is not retried.
    /// </para>
    /// <para>
    /// Each handler instance keeps its own retry budget for a request, separate from the budget of <see cref="HttpRestClient.BackoffStrategy"/>
    /// for connection failures and from the budgets of other handlers. A request that fails in several ways can therefore be retried more times
    /// in total than any single budget allows.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    /// <seealso cref="HttpRestClient.BackoffStrategy"/>
    public abstract class RetryableHttpErrorHandler : IHttpErrorHandler
    {
        private TimeSpan? _maxRetryDelay = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the longest wait before a retry that this handler accepts when the server suggests a retry time.
        /// </summary>
        /// <value>
        /// The longest accepted wait, or <see langword="null"/> to accept any suggested retry time. The default is five minutes.
        /// </value>
        /// <remarks>
        /// <para>
        /// When the response suggests a retry time, for example in a <c>Retry-After</c> header, and that time is further away than this value,
        /// the handler does not retry the request, and the <see cref="HttpResponseException"/> for the response reaches the caller. In that case,
        /// <see cref="OnBackoffStrategy"/> is not called.
        /// </para>
        /// <para>
        /// This limit applies only to retry times suggested by the server. It does not limit the delays of a backoff strategy, such as the
        /// <see cref="HttpRestClient.BackoffStrategy"/> used when the response suggests no retry time.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is negative.</exception>
        public TimeSpan? MaxRetryDelay
        {
            get => _maxRetryDelay;
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The maximum retry delay cannot be negative.");

                _maxRetryDelay = value;
            }
        }

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
        /// <returns>An <see cref="IRetrySession"/> that schedules the retry attempts, or <see langword="null"/> if the request must not be retried.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If the response suggests a retry time further away than <see cref="MaxRetryDelay"/>, the method returns <see langword="null"/>,
        /// so the request is not retried. Otherwise, the method uses <see cref="OnBackoffStrategy"/> when available. If the delegate is not
        /// provided or returns <see langword="null"/>, and the response includes a suggested retry time, a single retry at that time is used.
        /// Otherwise the client's default backoff strategy is used.
        /// </remarks>
        protected virtual IRetrySession? CreateScheduler(HttpResponseErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            var retryTime = GetSuggestedRetryTime(ctx);
            if (retryTime.HasValue && _maxRetryDelay.HasValue && retryTime.Value - DateTimeOffset.UtcNow > _maxRetryDelay.Value)
                return null;

            var strategy = OnBackoffStrategy?.Invoke(ctx, retryTime) ?? GetDefaultStrategy(ctx, retryTime);
            return strategy.CreateScheduler(ctx);
        }

        /// <inheritdoc/>
        Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            return ctx.ScheduleRetryAsync(this, CreateScheduler, cancellationToken);
        }
    }
}
