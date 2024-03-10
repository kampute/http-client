// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient.NewtonsoftJson package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.NewtonsoftJson
{
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents HTTP content based on JSON serialized from an object.
    /// </summary>
    public sealed class JsonContent : HttpContent
    {
        private static readonly Encoding utf8WithoutMarker = new UTF8Encoding(false);

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
                CharSet = utf8WithoutMarker.WebName
            };
        }

        /// <summary>
        /// Gets or sets the JSON serialization settings.
        /// </summary>
        /// <value>
        /// The JSON serialization settings, if any.
        /// </value>
        public JsonSerializerSettings? Settings { get; set; }

        /// <inheritdoc/>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using var streamWriter = new StreamWriter(stream, utf8WithoutMarker, 4096, true);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            var serializer = JsonSerializer.CreateDefault(Settings);
            serializer.Serialize(jsonWriter, _content);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
