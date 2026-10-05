// Copyright (C) 2025 Kampute
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
    /// Handles error responses with a function, without defining a handler class.
    /// </summary>
    /// <remarks>
    /// The handler accepts every status code, so it is asked about every error response that the handlers added before it do not retry. The function
    /// decides, for example by checking <see cref="HttpResponseErrorContext.Response"/>, whether to return a request to retry.
    /// </remarks>
    /// <example>
    /// This handler retries a request up to three times, one second apart, while the server answers '409 Conflict'. The policy object is the
    /// source of the retries, so they are counted separately from the retries of other handlers.
    /// <code>
    /// var conflictRetries = RetryStrategies.Constant(TimeSpan.FromSeconds(1)).WithMaxRetries(3).ToHttpRetryPolicy();
    ///
    /// client.ErrorHandlers.Add(new DynamicHttpErrorHandler((ctx, cancellationToken) =>
    ///     ctx.Response.StatusCode == HttpStatusCode.Conflict
    ///         ? ctx.ScheduleRetryAsync(conflictRetries, errorContext => conflictRetries.CreateSession(errorContext), cancellationToken)
    ///         : Task.FromResult(HttpErrorHandlerResult.NoRetry)));
    /// </code>
    /// </example>
    public class DynamicHttpErrorHandler : IHttpErrorHandler
    {
        private readonly Func<HttpResponseErrorContext, CancellationToken, Task<HttpErrorHandlerResult>> _asyncHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicHttpErrorHandler"/> class.
        /// </summary>
        /// <param name="asyncHandler">
        /// The function that decides. It receives the context of the error response and a cancellation token, and returns the decision.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncHandler"/> is <see langword="null"/>.</exception>
        public DynamicHttpErrorHandler(Func<HttpResponseErrorContext, CancellationToken, Task<HttpErrorHandlerResult>> asyncHandler)
        {
            _asyncHandler = asyncHandler ?? throw new ArgumentNullException(nameof(asyncHandler));
        }

        /// <summary>
        /// Determines whether the handler is capable of handling the provided HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns>Always <see langword="true"/>.</returns>
        public bool CanHandle(HttpStatusCode statusCode) => true;

        /// <summary>
        /// Calls the function of this handler to decide whether to retry a request after an error response.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>, indicating whether the request should be retried.</returns>
        protected virtual Task<HttpErrorHandlerResult> DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            return _asyncHandler(ctx, cancellationToken);
        }

        /// <inheritdoc/>
        Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            return DecideOnRetryAsync(ctx, cancellationToken);
        }
    }
}
