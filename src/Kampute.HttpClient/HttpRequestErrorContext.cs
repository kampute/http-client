// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Represents the context of an HTTP request error, encapsulating details about the request, the error encountered, and the client that sent the request.
    /// </summary>
    public class HttpRequestErrorContext
    {
        /// <summary>
        /// Initializes an instance of the <see cref="HttpRequestErrorContext"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance used to send the request.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that resulted in a failure.</param>
        /// <param name="error">The <see cref="Exception"/> containing details of the error encountered during the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="request"/> or <paramref name="error"/> or is <c>null</c>.</exception>
        public HttpRequestErrorContext(HttpRestClient client, HttpRequestMessage request, HttpRequestException error)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        /// <summary>
        /// Gets the <see cref="HttpRestClient"/> instance used to send the request.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRestClient"/> instance used to send the request.
        /// </value>
        public HttpRestClient Client { get; }

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> that resulted in a failure.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRequestMessage"/> that resulted in a failure.
        /// </value>
        public HttpRequestMessage Request { get; }

        /// <summary>
        /// Gets the <see cref="HttpRequestException"/> containing details of the error encountered during the HTTP request.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRequestException"/> containing details of the error encountered during the HTTP request.
        /// </value>
        public HttpRequestException Error { get; }
    }
}
