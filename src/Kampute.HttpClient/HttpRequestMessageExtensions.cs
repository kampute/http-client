// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System.Net.Http;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides extension methods for <see cref="HttpRequestMessage"/> to enhance functionality related to HTTP request processing.
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Clones the specified <see cref="HttpRequestMessage"/>, including its headers, version, and properties.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to clone.</param>
        /// <returns>A new instance of <see cref="HttpRequestMessage"/> that is a clone of the original.</returns>
        /// <remarks>
        /// This method copies the provided <see cref="HttpRequestMessage"/>, including its headers, version, and properties. The method reuses 
        /// the original request's <see cref="HttpContent"/> in the cloned request. 
        /// </remarks>
        public static HttpRequestMessage Clone(this HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version,
                Content = request.Content, // Content is reused, not cloned.
            };

            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            foreach (var property in request.Properties)
                clone.Properties.Add(property);

            clone.Properties[HttpRequestMessagePropertyKeys.CloneGeneration] = request.GetCloneGeneration() + 1;

            return clone;
        }

        /// <summary>
        /// Checks if the <see cref="HttpRequestMessage"/> is a clone.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to check.</param>
        /// <returns><c>true</c> if the request has been cloned; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A cloned request is one that has been created through the <see cref="Clone(HttpRequestMessage)"/> extension method.
        /// </remarks>
        /// <seealso cref="Clone(HttpRequestMessage)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCloned(this HttpRequestMessage request)
        {
            return request.Properties.ContainsKey(HttpRequestMessagePropertyKeys.CloneGeneration);
        }

        /// <summary>
        /// Retrieves the number of times the original request was cloned to produce this <see cref="HttpRequestMessage"/> instance.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to check.</param>
        /// <returns>The clone generation count or zero if the request has not been cloned.</returns>
        /// <remarks>
        /// The clone generation increases by 1 every time the request is cloned. An original request that has not been cloned
        /// will have a generation count of 0.
        /// </remarks>
        /// <seealso cref="Clone(HttpRequestMessage)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCloneGeneration(this HttpRequestMessage request)
        {
            return request.Properties.TryGetValue(HttpRequestMessagePropertyKeys.CloneGeneration, out var cloneGeneration) ? (int)cloneGeneration : 0;
        }
    }
}
