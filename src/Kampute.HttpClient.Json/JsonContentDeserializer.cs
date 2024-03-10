// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient.Json package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Json
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides functionality for deserializing JSON content from HTTP responses into objects.
    /// </summary>
    public sealed class JsonContentDeserializer : IHttpContentDeserializer
    {
        /// <summary>
        /// Gets or sets the JSON deserialization options.
        /// </summary>
        /// <value>
        /// The JSON deserialization options, if any.
        /// </value>
        public JsonSerializerOptions? Options { get; set; }

        /// <summary>
        /// Gets the collection of media types that this deserializer supports.
        /// </summary>
        /// <value>
        /// The collection of media types that this deserializer supports.
        /// </value>
        public IReadOnlyCollection<string> SupportedMediaTypes { get; } = [MediaTypeNames.Application.Json];

        /// <summary>
        /// Retrieves a collection of supported media types for a specific model type.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <returns>A read-only collection of strings representing the media types supported for the specified model type.</returns>
        public IReadOnlyCollection<string> GetSupportedMediaTypes(Type? modelType)
        {
            return modelType is not null ? SupportedMediaTypes : Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether this deserializer can handle data of a specific content type and deserialize it into the specified model type.
        /// </summary>
        /// <param name="mediaType">The media type of the content.</param>
        /// <param name="modelType">The type of the model to be deserialized.</param>
        /// <returns><c>true</c> if this deserializer can handle the specified content type and model type; otherwise, <c>false</c>.</returns>
        public bool CanDeserialize(string mediaType, Type? modelType)
        {
            return modelType is not null && SupportedMediaTypes.Contains(mediaType);
        }

        /// <summary>
        /// Asynchronously reads an object from the provided <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to read from.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <param name="cancellationToken">A token for canceling the read operation (optional).</param>
        /// <returns>A task representing the asynchronous read operation, containing the deserialized object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> or <paramref name="modelType"/> is <c>null</c>.</exception>
        public async Task<object?> DeserializeAsync(HttpContent content, Type modelType, CancellationToken cancellationToken = default)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (modelType == null)
                throw new ArgumentNullException(nameof(modelType));

            var encoding = content.FindCharacterEncoding();

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
