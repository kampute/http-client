// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient.Xml package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Xml
{
    using Kampute.HttpClient.Content.Abstracts;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Provides functionality for deserializing XML content from HTTP responses into objects.
    /// </summary>
    public sealed class XmlContentDeserializer : HttpContentDeserializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContentDeserializer"/> class.
        /// </summary>
        public XmlContentDeserializer()
            : base(MediaTypeNames.Application.Xml)
        {
        }

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

            using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            using var streamReader = new StreamReader(stream, encoding);
            using var xmlReader = XmlReader.Create(streamReader);
            return new XmlSerializer(modelType).Deserialize(xmlReader);
        }
    }
}
