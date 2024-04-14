// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient.Xml package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Xml
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Represents HTTP content based on XML serialized from an object.
    /// </summary>
    public sealed class XmlContent : HttpContent
    {
        private static readonly Encoding utf8WithoutMarker = new UTF8Encoding(false);

        private readonly object _content;
        private readonly Encoding _encoding;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContent"/> class using a specified content object with UTF-8 encoding.
        /// </summary>
        /// <param name="payload">The object to be serialized into XML format.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="payload"/> is <c>null</c>.</exception>
        public XmlContent(object payload)
            : this(payload, utf8WithoutMarker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContent"/> class using a specified content object and encoding.
        /// </summary>
        /// <param name="content">The object to be serialized into XML format.</param>
        /// <param name="encoding">The character encoding to use for the serialized XML content.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> or <paramref name="encoding"/> is <c>null</c>.</exception>
        public XmlContent(object content, Encoding encoding)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

            Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Xml)
            {
                CharSet = encoding.WebName
            };
        }

        /// <summary>
        /// Gets the character encoding of the serialized XML content.
        /// </summary>
        /// <value>
        /// The character encoding of the serialized XML content.
        /// </value>
        public Encoding Encoding => _encoding;

        /// <inheritdoc/>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using var streamWriter = new StreamWriter(stream, _encoding, 4096, true);
            using var xmlWriter = XmlWriter.Create(streamWriter, new XmlWriterSettings
            {
                Encoding = _encoding,
                OmitXmlDeclaration = false,
                CheckCharacters = true,
                Indent = false,
            });
            var serializer = new XmlSerializer(_content.GetType());
            serializer.Serialize(xmlWriter, _content);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
