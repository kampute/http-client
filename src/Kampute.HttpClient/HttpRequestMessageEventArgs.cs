// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Provides the request of the <see cref="HttpRestClient.BeforeSendingRequest"/> event.
    /// </summary>
    public class HttpRequestMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestMessageEventArgs"/> class with the specified request message.
        /// </summary>
        /// <param name="request">The request about to be sent.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="request"/> is <see langword="null"/>.</exception>
        public HttpRequestMessageEventArgs(HttpRequestMessage request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        /// <summary>
        /// Gets the request about to be sent.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRequestMessage"/> about to be sent. Changes to it are sent.
        /// </value>
        public HttpRequestMessage Request { get; }
    }
}
