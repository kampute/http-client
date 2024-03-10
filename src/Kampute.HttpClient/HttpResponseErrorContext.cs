// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Represents the context of an HTTP response error, providing information about the HTTP request, the client that sent the request, 
    /// the error encountered, and the response that indicates failure.
    /// </summary>
    public class HttpResponseErrorContext : HttpRequestErrorContext
    {
        /// <summary>
        /// Initializes an instance of the <see cref="HttpResponseErrorContext"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance used to send the request.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that resulted in a failure.</param>
        /// <param name="response">The <see cref="HttpResponseMessage"/> indicating the failure.</param>
        /// <param name="error">The <see cref="Exception"/> containing details of the error encountered during the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="request"/>, <paramref name="response"/> or <paramref name="error"/> or is <c>null</c>.</exception>
        public HttpResponseErrorContext(HttpRestClient client, HttpRequestMessage request, HttpResponseMessage response, HttpResponseException error)
            : base(client, request, error)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/> indicating the failure.
        /// </summary>
        /// <value>
        /// The <see cref="HttpResponseMessage"/> indicating the failure.
        /// </value>
        public HttpResponseMessage Response { get; }

        /// <summary>
        /// Gets the <see cref="HttpResponseException"/> containing details of the HTTP response error.
        /// </summary>
        /// <value>
        /// The <see cref="HttpResponseException"/> containing details of the HTTP response error.
        /// </value>
        public new HttpResponseException Error => (HttpResponseException)base.Error;
    }
}
