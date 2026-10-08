// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient.NewtonsoftJson package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.NewtonsoftJson
{
    using Kampute.HttpClient.Content.Abstracts;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Reads and writes <c>application/json</c> content with <c>Newtonsoft.Json</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The formatter also reads any media type with the <c>+json</c> structured syntax suffix, which RFC 6839 permits for media types whose
    /// representation follows <c>application/json</c>, such as <c>application/vnd.example+json</c>, but does not advertise it. It writes
    /// <c>application/json</c> only.
    /// </para>
    /// <para>
    /// The client reads a response with the first formatter in <see cref="HttpRestClient.ContentFormatters"/> that can read it, so a formatter for a
    /// specific <c>+json</c> media type takes over that type only when it is added before this one.
    /// </para>
    /// <para>
    /// Register the formatter with <see cref="HttpRestClientJsonExtensions.UseNewtonsoftJson"/>. Its <see cref="Settings"/> apply both to the responses
    /// it reads and to the payloads it writes. Change them before the client sends requests, because a registered formatter is shared by all requests
    /// of the client.
    /// </para>
    /// </remarks>
    public sealed class NewtonsoftJsonFormatter : HttpContentFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewtonsoftJsonFormatter"/> class.
        /// </summary>
        public NewtonsoftJsonFormatter()
            : base([MediaTypeNames.Application.Json], [MediaTypeNames.Application.Json])
        {
        }

        /// <summary>
        /// Gets or sets the JSON serializer settings.
        /// </summary>
        /// <value>
        /// The settings used to read responses and write payloads, or <see langword="null"/> for the defaults of <see cref="JsonSerializer"/>.
        /// </value>
        public JsonSerializerSettings? Settings { get; set; }

        /// <summary>
        /// Determines whether this formatter can read content of the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type of the content, without parameters.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="mediaType"/> is one of the <see cref="HttpContentFormatter.ReadableMediaTypes"/> or has the
        /// <c>+json</c> structured syntax suffix; otherwise, <see langword="false"/>.
        /// </returns>
        protected override bool CanReadMediaType(string mediaType)
        {
            return base.CanReadMediaType(mediaType) || HasStructuredSyntaxSuffix(mediaType, "+json");
        }

        /// <summary>
        /// Asynchronously reads an object of the specified type from JSON content.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to read.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to the object read from <paramref name="content"/>.</returns>
        /// <remarks>
        /// The content is decoded with the character set of its <c>Content-Type</c> header, or as UTF-8 if it has none.
        /// </remarks>
        protected override async Task<object?> ReadContentAsync(HttpContent content, Type modelType, CancellationToken cancellationToken)
        {
            var encoding = content.FindCharacterEncoding() ?? Encoding.UTF8;

            using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            using var streamReader = new StreamReader(stream, encoding);
            using var jsonReader = new JsonTextReader(streamReader);
            var serializer = JsonSerializer.CreateDefault(Settings);
            return serializer.Deserialize(jsonReader, modelType);
        }

        /// <summary>
        /// Creates JSON content that carries the specified payload.
        /// </summary>
        /// <param name="payload">The object to write.</param>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <returns>A <see cref="NewtonsoftJsonContent"/> with the <see cref="Settings"/> of this formatter.</returns>
        protected override HttpContent CreateContent(object payload, string mediaType)
        {
            return new NewtonsoftJsonContent(payload) { Settings = Settings };
        }
    }
}
