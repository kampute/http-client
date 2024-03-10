
// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Provides event data for events related to the receipt of HTTP responses.
    /// </summary>
    /// <remarks>
    /// This class is typically used in scenarios where an application needs to process or inspect HTTP responses in a centralized 
    /// manner. It encapsulates an instance of <see cref="HttpResponseMessage"/>, allowing event handlers to access and potentially 
    /// modify the response message. This capability is particularly useful in middle-ware, HTTP client wrappers, or other scenarios 
    /// where responses need to be logged, modified, or inspected for specific criteria (like status codes or headers) before being 
    /// processed further.
    /// </remarks>
    public class HttpResponseMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseMessageEventArgs"/> class with the specified response message.
        /// </summary>
        /// <param name="response">The received HTTP response message.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="response"/> is <c>null</c>.</exception>
        public HttpResponseMessageEventArgs(HttpResponseMessage response)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        /// <summary>
        /// Gets the HTTP response message.
        /// </summary>
        /// <value>
        /// The HTTP response message involved in the event.
        /// </value>
        public HttpResponseMessage Response { get; }
    }
}
