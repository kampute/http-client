// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient.DataContract package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.DataContract
{
    using Kampute.HttpClient.Content.Abstracts;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides functionality for deserializing XML content from HTTP responses into objects.
    /// </summary>
    public sealed class XmlContentDeserializer : HttpContentFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContentDeserializer"/> class.
        /// </summary>
        public XmlContentDeserializer()
            : base([MediaTypeNames.Application.Xml], [])
        {
        }

        /// <summary>
        /// Gets or sets the XML deserialization settings.
        /// </summary>
        /// <value>
        /// The XML deserialization settings, if any.
        /// </value>
        public DataContractSerializerSettings? Settings { get; set; }

        /// <summary>
        /// Determines whether the model type is marked with a <see cref="DataContractAttribute"/>.
        /// </summary>
        /// <param name="modelType">The type of the object to read.</param>
        /// <returns><see langword="true"/> if <paramref name="modelType"/> is marked with a <see cref="DataContractAttribute"/>; otherwise, <see langword="false"/>.</returns>
        protected override bool CanReadType(Type modelType)
        {
            return modelType.GetCustomAttribute<DataContractAttribute>() is not null;
        }

        /// <summary>
        /// Asynchronously reads an object from the provided <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to read from.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <param name="cancellationToken">A token for canceling the read operation.</param>
        /// <returns>A task representing the asynchronous read operation, containing the deserialized object.</returns>
        protected override async Task<object?> ReadContentAsync(HttpContent content, Type modelType, CancellationToken cancellationToken)
        {
            var encoding = content.FindCharacterEncoding() ?? Encoding.UTF8;

            using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            using var streamReader = new StreamReader(stream, encoding);
            using var xmlReader = XmlReader.Create(streamReader);
            return new DataContractSerializer(modelType, Settings).ReadObject(xmlReader);
        }
    }
}
