// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient.DataContract package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.DataContract
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="payload"/> is <see langword="null"/>.</exception>
        public XmlContent(object payload)
            : this(payload, utf8WithoutMarker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContent"/> class using a specified content object and encoding.
        /// </summary>
        /// <param name="content">The object to be serialized into XML format.</param>
        /// <param name="encoding">The character encoding to use for the serialized XML content.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> or <paramref name="encoding"/> is <see langword="null"/>.</exception>
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

        /// <summary>
        /// Gets or sets the XML serialization settings.
        /// </summary>
        /// <value>
        /// The XML serialization settings, if any.
        /// </value>
        public DataContractSerializerSettings? Settings { get; set; }

        /// <summary>
        /// Serializes the content to a stream asynchronously.
        /// </summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="context">The transport context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
            var serializer = new DataContractSerializer(_content.GetType(), Settings);
            serializer.WriteObject(xmlWriter, _content);
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
