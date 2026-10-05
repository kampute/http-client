// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Provides extension methods for <see cref="HttpRequestMessage"/> that error handlers use to retry a request.
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Creates a copy of a request to send again.
        /// </summary>
        /// <param name="request">The request to copy.</param>
        /// <returns>A new <see cref="HttpRequestMessage"/> with the method, URI, version, headers, and properties of <paramref name="request"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the content of the request cannot be sent again; see <see cref="CanClone"/>.</exception>
        /// <remarks>
        /// The copy shares the content of the original request rather than copying it, so disposing either request disposes the content of both.
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

            var cloneProperties = clone.GetPropertyBag();
            foreach (var property in request.GetPropertyBag())
                cloneProperties.Add(property);

            cloneProperties[HttpRequestMessagePropertyKeys.CloneGeneration] = request.GetCloneGeneration() + 1;

            return clone;
        }

        /// <summary>
        /// Determines whether a request can be copied with <see cref="Clone"/> and sent again.
        /// </summary>
        /// <param name="request">The request to check.</param>
        /// <returns>
        /// <see langword="true"/> if the request has no content or its content can be sent again; otherwise, <see langword="false"/>.
        /// </returns>
        /// <seealso cref="HttpContentExtensions.IsReusable"/>
        public static bool CanClone(this HttpRequestMessage request)
        {
            return request.Content is null || request.Content.IsReusable();
        }

        /// <summary>
        /// Determines whether a request is a copy made with <see cref="Clone(HttpRequestMessage)"/>.
        /// </summary>
        /// <param name="request">The request to check.</param>
        /// <returns><see langword="true"/> if the request is a copy; otherwise, <see langword="false"/>.</returns>
        /// <seealso cref="Clone(HttpRequestMessage)"/>
        public static bool IsCloned(this HttpRequestMessage request)
        {
            return request.GetPropertyBag().ContainsKey(HttpRequestMessagePropertyKeys.CloneGeneration);
        }

        /// <summary>
        /// Returns how many copies separate a request from the original request.
        /// </summary>
        /// <param name="request">The request to check.</param>
        /// <returns>
        /// 0 for an original request, 1 for a copy of it, 2 for a copy of that copy, and so on. Because each retry of a call clones the request
        /// last sent, this is usually the number of retries so far.
        /// </returns>
        /// <seealso cref="Clone(HttpRequestMessage)"/>
        public static int GetCloneGeneration(this HttpRequestMessage request)
        {
            return request.GetPropertyBag().TryGetValue(HttpRequestMessagePropertyKeys.CloneGeneration, out var cloneGeneration) && cloneGeneration is int generation ? generation : 0;
        }
    }
}
