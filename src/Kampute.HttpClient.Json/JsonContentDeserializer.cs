// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient.Json package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Json
{
    using Kampute.HttpClient.Content.Abstracts;
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides functionality for deserializing JSON content from HTTP responses into objects.
    /// </summary>
    public sealed class JsonContentDeserializer : HttpContentDeserializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonContentDeserializer"/> class.
        /// </summary>
        public JsonContentDeserializer()
            : base(MediaTypeNames.Application.Json)
        {
        }

        /// <summary>
        /// Gets or sets the JSON deserialization options.
        /// </summary>
        /// <value>
        /// The JSON deserialization options, if any.
        /// </value>
        public JsonSerializerOptions? Options { get; set; }

        /// <summary>
        /// Asynchronously reads an object from the provided <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to read from.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <param name="cancellationToken">A token for canceling the read operation (optional).</param>
        /// <returns>A task representing the asynchronous read operation, containing the deserialized object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> or <paramref name="modelType"/> is <see langword="null"/>.</exception>
        public override async Task<object?> DeserializeAsync(HttpContent content, Type modelType, CancellationToken cancellationToken = default)
        {
            if (content is null)
                throw new ArgumentNullException(nameof(content));
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            var encoding = content.FindCharacterEncoding() ?? Encoding.UTF8;

            if (encoding == Encoding.UTF8)
            {
                using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync(stream, modelType, Options, cancellationToken).ConfigureAwait(false);
            }

            var jsonString = await content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize(jsonString, modelType, Options);
        }
    }
}
