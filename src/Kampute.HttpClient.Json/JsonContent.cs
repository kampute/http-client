// Copyright (C) 2024 Kampute
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> is <c>null</c>.</exception>
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

        /// <inheritdoc/>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return JsonSerializer.SerializeAsync(stream, _content, Options);
        }

        /// <inheritdoc/>
        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
