// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient.Json package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Json
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents HTTP content based on JSON serialized from an object.
    /// </summary>
    public sealed class JsonContent : HttpContent
    {
        private readonly object _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonContent"/> class.
        /// </summary>
        /// <param name="content">The object to be serialized into JSON format.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> is <see langword="null"/>.</exception>
        public JsonContent(object content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));

            Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json)
            {
                CharSet = Encoding.UTF8.WebName
            };
        }

        /// <summary>
        /// Gets or sets the JSON serialization options.
        /// </summary>
        /// <value>
        /// The JSON serialization options, if any.
        /// </value>
        public JsonSerializerOptions? Options { get; set; }

        /// <summary>
        /// Serializes the content to a stream asynchronously.
        /// </summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="context">The transport context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return JsonSerializer.SerializeAsync(stream, _content, Options);
        }

        /// <summary>
        /// Attempts to compute the length of the content.
        /// </summary>
        /// <param name="length">When this method returns, contains the length of the content in bytes.</param>
        /// <returns><see langword="true"/> if the length could be computed; otherwise, <see langword="false"/>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
