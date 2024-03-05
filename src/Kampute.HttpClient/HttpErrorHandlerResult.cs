// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents the outcome of an HTTP error handling attempt by a specific handler, indicating whether it determines a failed request 
    /// should be retried.
    /// </summary>
    /// <remarks>
    /// This struct communicates the decision of an HTTP error handler regarding the handling of a failed request. It specifies whether the 
    /// handler determines the request should be retried, potentially with modifications, or if it considers the error not recoverable by 
    /// its logic, indicating that a retry should not be attempted. This determination is contextual to the handler's implementation and does 
    /// not preclude other handlers from potentially retrying the request.
    /// </remarks>
    public readonly struct HttpErrorHandlerResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpErrorHandlerResult"/> struct with a request to retry.
        /// </summary>
        /// <param name="requestToRetry">The <see cref="HttpRequestMessage"/> to use for retrying the failed request.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="requestToRetry"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HttpErrorHandlerResult(HttpRequestMessage requestToRetry)
        {
            RequestToRetry = requestToRetry ?? throw new ArgumentNullException(nameof(requestToRetry));
        }

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> to use for retrying the failed request, if the handler determines a retry is warranted; 
        /// otherwise, <c>null</c>.
        /// </summary>
        public readonly HttpRequestMessage? RequestToRetry;

        /// <summary>
        /// Creates a result indicating that the request should be retried with the provided <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="requestToRetry">The request to use for the retry.</param>
        /// <returns>An <see cref="HttpErrorHandlerResult"/> indicating the request should be retried with the provided <see cref="HttpRequestMessage"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="requestToRetry"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HttpErrorHandlerResult Retry(HttpRequestMessage requestToRetry) => new(requestToRetry);

        /// <summary>
        /// Represents a result indicating that the request should not be retried according to the handler's determination.
        /// </summary>
        public static readonly HttpErrorHandlerResult NoRetry = new();
    }
}
