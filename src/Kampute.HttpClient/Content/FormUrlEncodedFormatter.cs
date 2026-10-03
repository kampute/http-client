// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Content
{
    using Kampute.HttpClient.Content.Abstracts;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    /// <summary>
    /// Writes collections of key-value pairs as URL-encoded form content.
    /// </summary>
    /// <remarks>
    /// This formatter is send-only: it writes payloads that implement <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> with
    /// string keys and values as <c>application/x-www-form-urlencoded</c> content, and does not read responses. The form helpers of
    /// <see cref="HttpRestClientFormExtensions"/> use it.
    /// </remarks>
    public sealed class FormUrlEncodedFormatter : HttpContentFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormUrlEncodedFormatter"/> class.
        /// </summary>
        public FormUrlEncodedFormatter()
            : base([], [MediaTypeNames.Application.FormUrlEncoded])
        {
        }

        /// <summary>
        /// Determines whether the payload type is a collection of string key-value pairs.
        /// </summary>
        /// <param name="payloadType">The type of the payload to write.</param>
        /// <returns><see langword="true"/> if <paramref name="payloadType"/> implements <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> with string keys and values; otherwise, <see langword="false"/>.</returns>
        protected override bool CanWriteType(Type payloadType)
        {
            return typeof(IEnumerable<KeyValuePair<string, string>>).IsAssignableFrom(payloadType);
        }

        /// <summary>
        /// Creates URL-encoded form content from the payload.
        /// </summary>
        /// <param name="payload">The collection of key-value pairs to write.</param>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <returns>A <see cref="FormUrlEncodedContent"/> that carries <paramref name="payload"/>.</returns>
        protected override HttpContent CreateContent(object payload, string mediaType)
        {
            return new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)payload);
        }
    }
}
