// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

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
        /// <exception cref="InvalidOperationException">Thrown if the request contains a content that cannot be reused.</exception>
        /// <remarks>
        /// This method copies the provided <see cref="HttpRequestMessage"/>, including its headers, version, and properties. The method reuses 
        /// the original request's <see cref="HttpContent"/> in the cloned request. 
        /// </remarks>
        public static HttpRequestMessage Clone(this HttpRequestMessage request)
        {
            if (!request.CanClone())
                throw new InvalidOperationException("Cloning requests with non-reusable content is not supported due to the risk of stream consumption.");

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
        /// Determines whether the <see cref="HttpRequestMessage"/> can be cloned without issues.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to check.</param>
        /// <returns><see langword="true"/> if the request does not contain a content or the content is reusable; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This is a quick check to prevent cloning of requests that contain one-time-use content, which could lead to unexpected behaviors
        /// such as empty request bodies or <see cref="InvalidOperationException"/>.
        /// </remarks>
        public static bool CanClone(this HttpRequestMessage request)
        {
            return request.Content is null || request.Content.IsReusable();
        }

        /// <summary>
        /// Checks if the <see cref="HttpRequestMessage"/> is a clone.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to check.</param>
        /// <returns><see langword="true"/> if the request has been cloned; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// A cloned request is one that has been created through the <see cref="Clone(HttpRequestMessage)"/> extension method.
        /// </remarks>
        /// <seealso cref="Clone(HttpRequestMessage)"/>
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
        public static int GetCloneGeneration(this HttpRequestMessage request)
        {
            return request.Properties.TryGetValue(HttpRequestMessagePropertyKeys.CloneGeneration, out var cloneGeneration) ? (int)cloneGeneration : 0;
        }
    }
}
