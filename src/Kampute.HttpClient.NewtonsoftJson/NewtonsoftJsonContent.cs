// Copyright (C) 2025 Kampute
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
    /// Represents <c>application/json</c> content that serializes an object with <c>Newtonsoft.Json</c> when it is sent.
    /// </summary>
    public sealed class NewtonsoftJsonContent : HttpContent
    {
        private static readonly Encoding utf8WithoutMarker = new UTF8Encoding(false);

        private readonly object _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewtonsoftJsonContent"/> class.
        /// </summary>
        /// <param name="content">The object to serialize.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> is <see langword="null"/>.</exception>
        public NewtonsoftJsonContent(object content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));

            Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json)
            {
                CharSet = utf8WithoutMarker.WebName
            };
        }

        /// <summary>
        /// Gets or sets the serializer settings.
        /// </summary>
        /// <value>
        /// The settings used to serialize the object, or <see langword="null"/> for the defaults of the serializer.
        /// </value>
        public JsonSerializerSettings? Settings { get; set; }

        /// <summary>
        /// Writes the object to a stream as JSON.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="context">The transport context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            using var streamWriter = new StreamWriter(stream, utf8WithoutMarker, 4096, true);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            var serializer = JsonSerializer.CreateDefault(Settings);
            serializer.Serialize(jsonWriter, _content);
            return Task.CompletedTask;
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
