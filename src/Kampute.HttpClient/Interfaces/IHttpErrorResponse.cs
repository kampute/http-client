// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using System.Net;

    /// <summary>
    /// Defines an error model that creates the exception for an error response.
    /// </summary>
    /// <remarks>
    /// Implement this interface on the type assigned to <see cref="HttpRestClient.ResponseErrorType"/> to turn the error details that an API returns,
    /// such as a message or validation errors, into the <see cref="HttpResponseException"/> that the client throws.
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
