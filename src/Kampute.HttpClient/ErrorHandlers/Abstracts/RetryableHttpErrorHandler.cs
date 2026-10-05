// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers.Abstracts
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.Resilience;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides the base class for error handlers that retry a request after a delay when it receives an error response with a transient
    /// status code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A derived class chooses the status codes it handles by overriding <see cref="CanHandle"/>, and can change where the suggested retry time
    /// is read from and which policy applies when <see cref="OnRetryPolicy"/> provides none.
    /// </para>
    /// <para>
    /// The handler decides how to retry when it handles the first error response of a call, and applies that decision to every later error response
    /// it handles during the same call:
    /// <list type="bullet">
    ///   <item><description>If <see cref="OnRetryPolicy"/> returns a policy, that policy decides whether and when the request is retried.</description></item>
    ///   <item><description>Otherwise, the policy returned by <see cref="GetDefaultPolicy"/> decides.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// By default, if the first error response suggests a retry time, for example in a <c>Retry-After</c> header, the request is retried once, at
    /// that time. If the retry receives another error response that this handler handles, the error reaches the caller, whatever retry time the new
    /// response suggests. If the first error response suggests no retry time, the policy of <see cref="GetDefaultPolicy"/> for that case applies to
    /// the rest of the call, and retry times suggested by later responses do not change its delays. Either way, a server cannot keep a call waiting
    /// by moving its suggested retry time further away.
    /// </para>
    /// <para>
    /// Every suggested retry time is checked against <see cref="MaxRetryDelay"/>, which is five minutes by default. If a response suggests a time
    /// further away, the request is not retried, even when an earlier response of the same call was.
    /// </para>
    /// <para>
    /// The handler counts only the retries it makes. Retries after connection failures, which <see cref="HttpRestClient.RetryPolicy"/> decides,
    /// and retries made by other handlers are counted separately, so a call that fails in several ways can be retried more times in total than
    /// any one of their limits allows.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    /// <seealso cref="HttpRestClient.RetryPolicy"/>
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
        /// When a response suggests a retry time, for example in a <c>Retry-After</c> header, and that time is further away than this value,
        /// the handler does not retry the request, and the <see cref="HttpResponseException"/> for the response reaches the caller. The check
        /// applies to every response the handler handles, including later responses of a call that it has already retried. When the first
        /// error response of a call fails the check, <see cref="OnRetryPolicy"/> is not called.
        /// </para>
        /// <para>
        /// This limit applies only to retry times suggested by the server. It does not limit the delays of a retry policy, such as the
        /// <see cref="HttpRestClient.RetryPolicy"/> used when the response suggests no retry time.
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
        /// Gets or sets a function that chooses the retry policy of this handler for a call.
        /// </summary>
        /// <value>
        /// A function that receives the context of the error response and the retry time the response suggests, and returns the
        /// <see cref="IHttpRetryPolicy"/> to use, or <see langword="null"/> to use the policy of <see cref="GetDefaultPolicy"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// The function is called once per call, for the first error response this handler handles. The policy it returns decides the retries
        /// of that response and of the later error responses this handler handles during the same call.
        /// </para>
        /// <para>
        /// The function receives the following parameters:
        /// <list type="bullet">
        ///   <item>
        ///     <term>context</term>
        ///     <description>The <see cref="HttpResponseErrorContext"/> of the error response.</description>
        ///   </item>
        ///   <item>
        ///     <term>retryTime</term>
        ///     <description>The retry time the response suggests, or <see langword="null"/> if it suggests none.</description>
        ///   </item>
        /// </list>
        /// </para>
        /// </remarks>
        public Func<HttpResponseErrorContext, DateTimeOffset?, IHttpRetryPolicy?>? OnRetryPolicy { get; set; }

        /// <summary>
        /// Determines whether this handler can process the specified HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns><see langword="true"/> if the handler can process the status code; otherwise, <see langword="false"/>.</returns>
        public abstract bool CanHandle(HttpStatusCode statusCode);

        /// <summary>
        /// Reads the retry time that an error response suggests.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response.</param>
        /// <returns>
        /// The time the response suggests for the retry, or <see langword="null"/> if it suggests none. The base implementation reads the
        /// <c>Retry-After</c> header.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        protected virtual DateTimeOffset? GetSuggestedRetryTime(HttpResponseErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            ctx.Response.Headers.TryExtractRetryAfterTime(out var retryTime);
            return retryTime;
        }

        /// <summary>
        /// Returns the retry policy of this handler for a call when <see cref="OnRetryPolicy"/> provides none.
        /// </summary>
        /// <param name="ctx">The context of the first error response this handler handles during the call.</param>
        /// <param name="retryTime">The retry time the response suggests, or <see langword="null"/> if it suggests none.</param>
        /// <returns>
        /// The base implementation returns a policy that retries once, at <paramref name="retryTime"/>, if the response suggests a retry time,
        /// and the <see cref="HttpRestClient.RetryPolicy"/> of the client otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        protected virtual IHttpRetryPolicy GetDefaultPolicy(HttpResponseErrorContext ctx, DateTimeOffset? retryTime)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            return retryTime.HasValue ? RetryStrategies.Once(retryTime.Value).ToHttpRetryPolicy() : ctx.Client.RetryPolicy;
        }

        /// <summary>
        /// Creates the retry session that decides the retries of this handler for a call.
        /// </summary>
        /// <param name="ctx">The context of the first error response this handler handles during the call.</param>
        /// <returns>An <see cref="IRetrySession"/> that decides on the retry attempts, or <see langword="null"/> if the request must not be retried.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// The handler calls this method only for the first error response it handles during a call. The later error responses it handles during
        /// the same call reuse the session this method returns.
        /// </para>
        /// <para>
        /// If the response suggests a retry time further away than <see cref="MaxRetryDelay"/>, the method returns <see langword="null"/>,
        /// so the request is not retried. Otherwise, it creates the session from the policy that <see cref="OnRetryPolicy"/> returns, or from
        /// the policy of <see cref="GetDefaultPolicy"/>.
        /// </para>
        /// </remarks>
        protected virtual IRetrySession? CreateSession(HttpResponseErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            var retryTime = GetSuggestedRetryTime(ctx);
            if (ExceedsMaxRetryDelay(retryTime))
                return null;

            var strategy = OnRetryPolicy?.Invoke(ctx, retryTime) ?? GetDefaultPolicy(ctx, retryTime);
            return strategy.CreateSession(ctx);
        }

        /// <inheritdoc/>
        Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            if (!ctx.Request.CanClone() || ExceedsMaxRetryDelay(GetSuggestedRetryTime(ctx)))
                return Task.FromResult(HttpErrorHandlerResult.NoRetry);

            return ctx.ScheduleRetryAsync(this, CreateSession, cancellationToken);
        }

        /// <summary>
        /// Checks the suggested retry time against the configured limit, including when a retry session already exists.
        /// </summary>
        /// <param name="retryTime">The retry time suggested by the current response, if any.</param>
        /// <returns><see langword="true"/> if the suggested time is further away than the configured limit.</returns>
        private bool ExceedsMaxRetryDelay(DateTimeOffset? retryTime)
        {
            return retryTime.HasValue && _maxRetryDelay.HasValue && retryTime.Value - DateTimeOffset.UtcNow > _maxRetryDelay.Value;
        }
    }
}
