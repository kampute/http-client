// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for handling HTTP error status codes and determining retry logic in HTTP requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a mechanism to extend the retry logic of the <see cref="HttpRestClient"/>. By implementing this interface, 
    /// consumers of the client can implement custom logic to evaluate failure responses and decide whether to attempt a retry.
    /// </para>
    /// <para>
    /// Implementors can define their own strategies for handling specific HTTP error statuses, such as '401 Unauthorized' for re-authentication, 
    /// '429 Too Many Requests' for rate limit handling, or '503 Service Unavailable' for backoff and retry. This flexible approach allows for 
    /// sophisticated error handling and recovery mechanisms, tailored to the requirements of the application.
    /// </para>
    /// <para>
    /// The implementations of <see cref="IHttpErrorHandler"/> should be thread-safe and reusable across multiple error handling operations to 
    /// facilitate efficient processing of HTTP responses in a concurrent environment.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public interface IHttpErrorHandler
    {
        /// <summary>
        /// Determines whether the handler is capable of handling the provided HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns><see langword="true"/>if the handler can handle the specified status code; otherwise, <see langword="false"/>.</returns>
        bool CanHandle(HttpStatusCode statusCode);

        /// <summary>
        /// Evaluates whether a failed request should be retried based on the error context.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        Task<HttpErrorHandlerResult> DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken);
    }
}
