// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    /// <summary>
    /// Provides the standard HTTP methods on every target framework.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Net.Http.HttpMethod"/> has no <c>Patch</c> property on .NET Standard 2.0; this class provides it there too.
    /// </remarks>
    public static class HttpVerb
    {
        /// <summary>
        /// The DELETE method, which removes the target resource.
        /// </summary>
        public readonly static System.Net.Http.HttpMethod Delete = System.Net.Http.HttpMethod.Delete;

        /// <summary>
        /// The GET method, which retrieves a representation of the target resource without changing it.
        /// </summary>
        public readonly static System.Net.Http.HttpMethod Get = System.Net.Http.HttpMethod.Get;

        /// <summary>
        /// The HEAD method, which is identical to GET except that the response has no body.
        /// </summary>
        public readonly static System.Net.Http.HttpMethod Head = System.Net.Http.HttpMethod.Head;

        /// <summary>
        /// The OPTIONS method, which asks for the communication options of the target resource, such as the methods it supports.
        /// </summary>
        public readonly static System.Net.Http.HttpMethod Options = System.Net.Http.HttpMethod.Options;

        /// <summary>
        /// The PATCH method, which applies a partial update to the target resource.
        /// </summary>
#if !NETSTANDARD2_0
        public readonly static System.Net.Http.HttpMethod Patch = System.Net.Http.HttpMethod.Patch;
#else
        public readonly static System.Net.Http.HttpMethod Patch = new("PATCH");
#endif

        /// <summary>
        /// The POST method, which submits the payload to the target resource for processing, such as to create a resource.
        /// </summary>
        public readonly static System.Net.Http.HttpMethod Post = System.Net.Http.HttpMethod.Post;

        /// <summary>
        /// The PUT method, which replaces the target resource with the payload.
        /// </summary>
        public readonly static System.Net.Http.HttpMethod Put = System.Net.Http.HttpMethod.Put;

        /// <summary>
        /// The TRACE method, which asks the server to echo the request it received, for diagnostics.
        /// </summary>
        public readonly static System.Net.Http.HttpMethod Trace = System.Net.Http.HttpMethod.Trace;
    }
}
