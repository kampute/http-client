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
    /// Defines a handler that decides whether to retry a request after an error response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a request receives an error response, <see cref="HttpRestClient"/> asks the handlers in <see cref="HttpRestClient.ErrorHandlers"/> whose
    /// <see cref="CanHandle"/> accepts the status code, in order, until one of them returns a request to retry. If none does, the error is thrown
    /// as an <see cref="HttpResponseException"/>. A handler can retry a clone of the failed request or a request it builds, for example one with
    /// new credentials after a '401 Unauthorized' response.
    /// </para>
    /// <para>
    /// A handler is shared by all the requests of the clients it is added to, so implementations must be thread-safe.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public interface IHttpErrorHandler
    {
        /// <summary>
        /// Determines whether the handler is capable of handling the provided HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns><see langword="true"/> if the handler can handle the specified status code; otherwise, <see langword="false"/>.</returns>
        bool CanHandle(HttpStatusCode statusCode);

        /// <summary>
        /// Decides whether to retry a request after an error response.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>
        /// A task that resolves to <see cref="HttpErrorHandlerResult.Retry"/> with the request to send, or to <see cref="HttpErrorHandlerResult.NoRetry"/>
        /// to let the next handler decide.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        Task<HttpErrorHandlerResult> DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken);
    }
}
