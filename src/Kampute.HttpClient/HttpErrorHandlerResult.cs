// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Represents the decision of an error handler: retry with a given request, or do not retry.
    /// </summary>
    /// <remarks>
    /// A <see cref="NoRetry"/> decision only means that this handler does not retry; the client then asks the next handler that can handle the
    /// status code.
    /// </remarks>
    public readonly struct HttpErrorHandlerResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpErrorHandlerResult"/> struct with a request to retry.
        /// </summary>
        /// <param name="requestToRetry">The <see cref="HttpRequestMessage"/> to use for retrying the failed request.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="requestToRetry"/> is <see langword="null"/>.</exception>
        private HttpErrorHandlerResult(HttpRequestMessage requestToRetry)
        {
            RequestToRetry = requestToRetry ?? throw new ArgumentNullException(nameof(requestToRetry));
        }

        /// <summary>
        /// The request to send as the retry, or <see langword="null"/> if the handler does not retry.
        /// </summary>
        public readonly HttpRequestMessage? RequestToRetry;

        /// <summary>
        /// Creates a decision to retry with the specified request.
        /// </summary>
        /// <param name="requestToRetry">
        /// The request to send, such as a clone of the failed request made with <see cref="HttpRequestMessageExtensions.Clone"/>, or a new request.
        /// </param>
        /// <returns>An <see cref="HttpErrorHandlerResult"/> whose <see cref="RequestToRetry"/> is <paramref name="requestToRetry"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="requestToRetry"/> is <see langword="null"/>.</exception>
        public static HttpErrorHandlerResult Retry(HttpRequestMessage requestToRetry) => new(requestToRetry);

        /// <summary>
        /// The decision not to retry.
        /// </summary>
        public static readonly HttpErrorHandlerResult NoRetry = new();
    }
}
