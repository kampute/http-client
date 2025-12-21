// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    /// <summary>
    /// A helper class for retrieving the standard HTTP methods.
    /// </summary>
    /// <remarks>
    /// This class supplements the standard <see cref="System.Net.Http.HttpMethod"/> class with additional, commonly 
    /// used HTTP methods that are not covered by the .NET Standard 2.0 specification.
    /// </remarks>
    public static class HttpVerb
    {
        /// <summary>
        /// Represents an HTTP DELETE protocol method.
        /// </summary>
        /// <remarks>
        /// The DELETE method requests that the target resource be removed. It is used to delete a resource identified 
        /// by a URI.
        /// </remarks>
        public readonly static System.Net.Http.HttpMethod Delete = System.Net.Http.HttpMethod.Delete;

        /// <summary>
        /// Represents an HTTP GET protocol method.
        /// </summary>
        /// <remarks>
        /// The GET method requests a representation of the specified resource. Requests using GET should only retrieve 
        /// data and should have no other effect.
        /// </remarks>
        public readonly static System.Net.Http.HttpMethod Get = System.Net.Http.HttpMethod.Get;

        /// <summary>
        /// Represents an HTTP HEAD protocol method.
        /// </summary>
        /// <remarks>
        /// The HEAD method is identical to GET except that the server responds with headers only and no message body. 
        /// It is often used for testing hypertext links for validity, accessibility, and recent modification.
        /// </remarks>
        public readonly static System.Net.Http.HttpMethod Head = System.Net.Http.HttpMethod.Head;

        /// <summary>
        /// Represents an HTTP OPTIONS protocol method.
        /// </summary>
        /// <remarks>
        /// The OPTIONS method describes the communication options for the target resource. It can be used to query 
        /// the server for supported HTTP methods and other options, without implying a resource action.
        /// </remarks>
        public readonly static System.Net.Http.HttpMethod Options = System.Net.Http.HttpMethod.Options;

        /// <summary>
        /// Represents an HTTP PATCH protocol method.
        /// </summary>
        /// <remarks>
        /// The PATCH method applies partial modifications to a resource. It is used to make a partial update on a resource, 
        /// in contrast to PUT which typically requires a complete resource representation.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER
        public readonly static System.Net.Http.HttpMethod Patch = System.Net.Http.HttpMethod.Patch;
#else
        public readonly static System.Net.Http.HttpMethod Patch = new("PATCH");
#endif

        /// <summary>
        /// Represents an HTTP POST protocol method.
        /// </summary>
        /// <remarks>
        /// The POST method is used to submit an entity to the specified resource, often causing a change in state or side 
        /// effects on the server.
        /// </remarks>
        public readonly static System.Net.Http.HttpMethod Post = System.Net.Http.HttpMethod.Post;

        /// <summary>
        /// Represents an HTTP PUT protocol method.
        /// </summary>
        /// <remarks>
        /// The PUT method replaces all current representations of the target resource with the request payload. It is 
        /// used to update a resource entirely.
        /// </remarks>
        public readonly static System.Net.Http.HttpMethod Put = System.Net.Http.HttpMethod.Put;

        /// <summary>
        /// Represents an HTTP TRACE protocol method.
        /// </summary>
        /// <remarks>
        /// The TRACE method performs a message loop-back test along the path to the target resource, providing a useful 
        /// debugging mechanism.
        /// </remarks>
        public readonly static System.Net.Http.HttpMethod Trace = System.Net.Http.HttpMethod.Trace;
    }
}
