// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Xml
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents HTTP content based on XML serialized from an object.
    /// </summary>
    /// <remarks>
    /// The object is serialized when the content is sent, by the serializer that <see cref="Serializer"/> selects for its runtime type. Use this class
    /// to send XML with <see cref="HttpRestClientExtensions.PostAsync{T}(HttpRestClient, string, HttpContent?, System.Threading.CancellationToken)"/> and
    /// the other helpers that take an <see cref="HttpContent"/>.
    /// </remarks>
    public sealed class XmlContent : HttpContent
    {
        private static readonly Encoding utf8WithoutMarker = new UTF8Encoding(false);

        private readonly object _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContent"/> class, encoded as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="content">The object to serialize into XML format.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> is <see langword="null"/>.</exception>
        public XmlContent(object content)
            : this(content, utf8WithoutMarker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContent"/> class with the specified character encoding.
        /// </summary>
        /// <param name="content">The object to serialize into XML format.</param>
        /// <param name="encoding">The character encoding of the XML.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> or <paramref name="encoding"/> is <see langword="null"/>.</exception>
        public XmlContent(object content, Encoding encoding)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

            Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Xml)
            {
                CharSet = encoding.WebName
            };
        }

        /// <summary>
        /// Gets the character encoding of the XML.
        /// </summary>
        /// <value>
        /// The character encoding of the XML.
        /// </value>
        public Encoding Encoding { get; }

        /// <summary>
        /// Gets or sets which serializer writes the object.
        /// </summary>
        /// <value>
        /// The serializer choice. The default is <see cref="XmlSerializerKind.Auto"/>, which uses <see cref="DataContractSerializer"/> for types marked
        /// with <see cref="DataContractAttribute"/> or <see cref="CollectionDataContractAttribute"/>, and <see cref="System.Xml.Serialization.XmlSerializer"/>
        /// for any other type.
        /// </value>
        public XmlSerializerKind Serializer { get; set; }

        /// <summary>
        /// Gets or sets the settings of <see cref="DataContractSerializer"/>.
        /// </summary>
        /// <value>
        /// The settings used when <see cref="DataContractSerializer"/> writes the object, or <see langword="null"/> for its defaults. They have no
        /// effect when <see cref="System.Xml.Serialization.XmlSerializer"/> writes the object.
        /// </value>
        public DataContractSerializerSettings? DataContractSettings { get; set; }

        /// <summary>
        /// Serializes the object to a stream.
        /// </summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="context">The transport context.</param>
        /// <returns>A completed task, because the object is serialized synchronously.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            XmlSerialization.Write(stream, Encoding, _content, Serializer, DataContractSettings);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Indicates that the length of the content is not known in advance.
        /// </summary>
        /// <param name="length">Always -1.</param>
        /// <returns>Always <see langword="false"/>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
