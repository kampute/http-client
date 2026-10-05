
// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Provides the response of the <see cref="HttpRestClient.AfterReceivingResponse"/> event.
    /// </summary>
    public class HttpResponseMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseMessageEventArgs"/> class with the specified response message.
        /// </summary>
        /// <param name="response">The response received.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="response"/> is <see langword="null"/>.</exception>
        public HttpResponseMessageEventArgs(HttpResponseMessage response)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        /// <summary>
        /// Gets the response received.
        /// </summary>
        /// <value>
        /// The <see cref="HttpResponseMessage"/> received, before the client checks its status code or reads its content.
        /// </value>
        public HttpResponseMessage Response { get; }
    }
}
