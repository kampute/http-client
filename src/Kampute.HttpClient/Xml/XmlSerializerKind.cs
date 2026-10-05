// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Xml
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Specifies which .NET serializer reads and writes XML content.
    /// </summary>
    /// <seealso cref="XmlFormatter.Serializer"/>
    /// <seealso cref="XmlContent.Serializer"/>
    public enum XmlSerializerKind
    {
        /// <summary>
        /// Chooses the serializer by type: a type marked with <see cref="DataContractAttribute"/> or <see cref="CollectionDataContractAttribute"/>
        /// uses <see cref="System.Runtime.Serialization.DataContractSerializer"/>, and any other type uses <see cref="System.Xml.Serialization.XmlSerializer"/>.
        /// </summary>
        Auto,

        /// <summary>
        /// Uses <see cref="System.Xml.Serialization.XmlSerializer"/> for every type, including types marked for data contracts.
        /// </summary>
        XmlSerializer,

        /// <summary>
        /// Uses <see cref="System.Runtime.Serialization.DataContractSerializer"/> for every type, including types without data contract attributes.
        /// </summary>
        DataContractSerializer,
    }
}
