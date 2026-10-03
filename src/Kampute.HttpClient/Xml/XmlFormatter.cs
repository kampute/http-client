// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Xml
{
    using Kampute.HttpClient.Content.Abstracts;
    using System;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Reads and writes <c>application/xml</c> content with <see cref="System.Xml.Serialization.XmlSerializer"/> or <see cref="DataContractSerializer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Serializer"/> property selects the serializer. The same rule applies when a response is read, by the requested model type, and when
    /// a payload is written, by the runtime type of the payload. With the default, <see cref="XmlSerializerKind.Auto"/>, a type marked with
    /// <see cref="DataContractAttribute"/> or <see cref="CollectionDataContractAttribute"/> uses <see cref="DataContractSerializer"/>, and any other type
    /// uses <see cref="System.Xml.Serialization.XmlSerializer"/>.
    /// </para>
    /// <para>
    /// Register the formatter with <see cref="HttpRestClientXmlExtensions.UseXml"/>. Change its properties before the client sends requests, because
    /// a registered formatter is shared by all requests of the client.
    /// </para>
    /// </remarks>
    public sealed class XmlFormatter : HttpContentFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFormatter"/> class.
        /// </summary>
        public XmlFormatter()
            : base([MediaTypeNames.Application.Xml], [MediaTypeNames.Application.Xml])
        {
        }

        /// <summary>
        /// Gets or sets which serializer reads and writes XML.
        /// </summary>
        /// <value>
        /// The serializer choice. The default is <see cref="XmlSerializerKind.Auto"/>.
        /// </value>
        public XmlSerializerKind Serializer { get; set; }

        /// <summary>
        /// Gets or sets the settings of <see cref="DataContractSerializer"/>.
        /// </summary>
        /// <value>
        /// The settings used in both directions when <see cref="DataContractSerializer"/> is selected, or <see langword="null"/> for its defaults.
        /// They have no effect when <see cref="System.Xml.Serialization.XmlSerializer"/> is selected.
        /// </value>
        public DataContractSerializerSettings? DataContractSettings { get; set; }

        /// <summary>
        /// Asynchronously reads an object of the specified type from XML content.
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
            return XmlSerialization.Read(stream, encoding, modelType, Serializer, DataContractSettings);
        }

        /// <summary>
        /// Creates XML content that carries the specified payload.
        /// </summary>
        /// <param name="payload">The object to write.</param>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <returns>An <see cref="XmlContent"/> with the <see cref="Serializer"/> and <see cref="DataContractSettings"/> of this formatter.</returns>
        protected override HttpContent CreateContent(object payload, string mediaType)
        {
            return new XmlContent(payload)
            {
                Serializer = Serializer,
                DataContractSettings = DataContractSettings,
            };
        }
    }
}
