// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Xml
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Reads and writes XML with the serializer that an <see cref="XmlSerializerKind"/> selects for a type.
    /// </summary>
    internal static class XmlSerialization
    {
        /// <summary>
        /// Determines whether the serializer choice selects <see cref="DataContractSerializer"/> for the specified type.
        /// </summary>
        /// <param name="kind">The serializer choice.</param>
        /// <param name="type">The type to read or write.</param>
        /// <returns><see langword="true"/> if <see cref="DataContractSerializer"/> is selected; <see langword="false"/> if <see cref="XmlSerializer"/> is.</returns>
        public static bool UsesDataContract(XmlSerializerKind kind, Type type) => kind switch
        {
            XmlSerializerKind.DataContractSerializer => true,
            XmlSerializerKind.XmlSerializer => false,
            _ => type.IsDefined(typeof(DataContractAttribute), inherit: false) || type.IsDefined(typeof(CollectionDataContractAttribute), inherit: false),
        };

        /// <summary>
        /// Reads an object of the specified type from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="encoding">
        /// The character encoding named by the <c>charset</c> parameter of the content, which a byte order mark overrides, or <see langword="null"/>
        /// to detect the encoding from a byte order mark or the XML encoding declaration, with UTF-8 as the default.
        /// </param>
        /// <param name="type">The type of the object to read.</param>
        /// <param name="kind">The serializer choice.</param>
        /// <param name="dataContractSettings">The settings of <see cref="DataContractSerializer"/>, if it is used.</param>
        /// <returns>The object read from <paramref name="stream"/>.</returns>
        public static object? Read(Stream stream, Encoding? encoding, Type type, XmlSerializerKind kind, DataContractSerializerSettings? dataContractSettings)
        {
            using var streamReader = encoding is not null ? new StreamReader(stream, encoding) : null;
            using var xmlReader = streamReader is not null ? XmlReader.Create(streamReader) : XmlReader.Create(stream);
            return UsesDataContract(kind, type)
                ? new DataContractSerializer(type, dataContractSettings).ReadObject(xmlReader)
                : new XmlSerializer(type).Deserialize(xmlReader);
        }

        /// <summary>
        /// Writes an object to a stream.
        /// </summary>
        /// <param name="stream">The stream to write.</param>
        /// <param name="encoding">The character encoding of the output.</param>
        /// <param name="value">The object to write.</param>
        /// <param name="kind">The serializer choice.</param>
        /// <param name="dataContractSettings">The settings of <see cref="DataContractSerializer"/>, if it is used.</param>
        public static void Write(Stream stream, Encoding encoding, object value, XmlSerializerKind kind, DataContractSerializerSettings? dataContractSettings)
        {
            using var streamWriter = new StreamWriter(stream, encoding, 4096, leaveOpen: true);
            using var xmlWriter = XmlWriter.Create(streamWriter, new XmlWriterSettings
            {
                Encoding = encoding,
                OmitXmlDeclaration = false,
                CheckCharacters = true,
                Indent = false,
            });

            var type = value.GetType();
            if (UsesDataContract(kind, type))
                new DataContractSerializer(type, dataContractSettings).WriteObject(xmlWriter, value);
            else
                new XmlSerializer(type).Serialize(xmlWriter, value);
        }
    }
}
