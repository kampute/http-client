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
    /// Provides a dynamic mechanism to handle HTTP error status codes and determine retry logic for HTTP requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements the <see cref="IHttpErrorHandler"/> interface, allowing for custom and dynamic error handling strategies to be defined at runtime. 
    /// It encapsulates a delegate that is invoked to determine the retry logic for failed HTTP requests, making it highly flexible and adaptable to various error 
    /// handling scenarios.
    /// </para>
    /// <para>
    /// Since this class always returns <see langword="true"/> for <see cref="CanHandle(HttpStatusCode)"/>, it represents a "catch-all" handler that can be used as a fall-back 
    /// when no other specific error handlers are suitable.
    /// </para>
    /// </remarks>
    public class DynamicHttpErrorHandler : IHttpErrorHandler
    {
        private readonly Func<HttpRequestErrorContext, CancellationToken, Task<HttpErrorHandlerResult>> _asyncHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicHttpErrorHandler"/> class.
        /// </summary>
        /// <param name="asyncHandler">The asynchronous delegate to handle HTTP error status codes and decide on retry logic.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncHandler"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The delegate receives the following parameters:
        /// <list type="bullet">
        /// <item>
        /// <term>context</term>
        /// <description>Provides context about the HTTP request resulting in a failure response. It is encapsulated within an 
        /// <see cref="HttpResponseErrorContext"/> instance, allowing for an informed decision on the retry strategy.</description>
        /// </item>
        /// <item>
        /// <term>cancellationToken</term>
        /// <description>A <see cref="CancellationToken"/> for canceling the operation.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public DynamicHttpErrorHandler(Func<HttpRequestErrorContext, CancellationToken, Task<HttpErrorHandlerResult>> asyncHandler)
        {
            _asyncHandler = asyncHandler ?? throw new ArgumentNullException(nameof(asyncHandler));
        }

        /// <summary>
        /// Determines whether the handler is capable of handling the provided HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns>Always <see langword="true"/>, indicating that this handler can handle any status code.</returns>
        public bool CanHandle(HttpStatusCode statusCode) => true;

        /// <summary>
        /// Invokes the configured asynchronous delegate to determine whether a failed request should be retried.
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
