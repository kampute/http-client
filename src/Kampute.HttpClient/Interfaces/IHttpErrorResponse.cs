// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using System.Net;

    /// <summary>
    /// Defines an interface for handling HTTP error responses and converting them into a <see cref="HttpResponseException"/>.
    /// </summary>
    /// <remarks>
    /// This interface is especially beneficial in RESTful operation contexts where the server provides error details in a 
    /// distinct format. It facilitates the conversion of these details into a structured <see cref="HttpResponseException"/>, 
    /// thereby enhancing error handling and its integration into client-side logic.
    /// </remarks>
    public interface IHttpErrorResponse
    {
        /// <summary>
        /// Converts the object into a <see cref="HttpResponseException"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        /// <returns>A <see cref="HttpResponseException"/> that represents the error.</returns>
        HttpResponseException ToException(HttpStatusCode statusCode);
    }
}
