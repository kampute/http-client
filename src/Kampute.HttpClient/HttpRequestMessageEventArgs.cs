// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Provides event data for events that involve manipulation or inspection of HTTP request messages.
    /// </summary>
    /// <remarks>
    /// This class is typically used in scenarios where an HTTP request message needs to be inspected or modified
    /// before it is sent. It encapsulates an instance of <see cref="HttpRequestMessage"/>, allowing subscribers
    /// of the event to access and manipulate the request as necessary. Common use cases include adding headers,
    /// changing the request URI, or modifying the request body.
    /// </remarks>
    public class HttpRequestMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestMessageEventArgs"/> class with the specified request message.
        /// </summary>
        /// <param name="request">The HTTP request message that has been created.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="request"/> is <see langword="null"/>.</exception>
        public HttpRequestMessageEventArgs(HttpRequestMessage request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        /// <summary>
        /// Gets the HTTP request message.
        /// </summary>
        /// <value>
        /// The HTTP request message involved in the event.
        /// </value>
        public HttpRequestMessage Request { get; }
    }
}
