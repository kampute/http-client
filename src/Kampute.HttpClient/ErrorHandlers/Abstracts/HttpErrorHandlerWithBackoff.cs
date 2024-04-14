// Copyright (C) 2024 Kampute
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
    /// An abstract class designed for HTTP error handlers that incorporate a backoff strategy to recover from errors.
    /// </summary>    
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public abstract class HttpErrorHandlerWithBackoff : IHttpErrorHandler
    {
        /// <summary>
        /// Determines whether this handler can process the specified HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns><c>true</c> if the handler can process the status code; otherwise, <c>false</c>.</returns>
        public abstract bool CanHandle(HttpStatusCode statusCode);

        /// <summary>
        /// Determines the backoff strategy to use based on the error context.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <returns>An <see cref="IRetrySchedulerFactory"/> that defines the backoff strategy.</returns>
        protected abstract IRetrySchedulerFactory DetermineBackoffStrategy(HttpResponseErrorContext ctx);

        /// <summary>
        /// Attempts to recover from a HTTP error by backing off and retrying the request.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method attempts to retry the request based on the backoff strategy determined by <see cref="DetermineBackoffStrategy(HttpResponseErrorContext)"/>. 
        /// It checks for the ability to retry the request, waits according to the backoff strategy, and then decides whether a retry should be attempted.
        /// </remarks>
        protected virtual async Task<HttpErrorHandlerResult> BackoffAndDecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            if (!ctx.Request.CanClone())
                return HttpErrorHandlerResult.NoRetry;

            var scheduler = ctx.Request.Properties.GetOrAdd(HttpRequestMessagePropertyKeys.RetryScheduler, _ => DetermineBackoffStrategy(ctx).CreateScheduler(ctx));
            if (!await scheduler.WaitAsync(cancellationToken).ConfigureAwait(false))
                return HttpErrorHandlerResult.NoRetry;

            return HttpErrorHandlerResult.Retry(ctx.Request.Clone());
        }

        /// <inheritdoc/>
        Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            return BackoffAndDecideOnRetryAsync(ctx, cancellationToken);
        }
    }
}
