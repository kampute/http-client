// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a content format that converts between HTTP content and .NET objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A formatter has a reading side and a writing side. The reading side turns response content into objects: <see cref="GetReadableMediaTypes"/>
    /// lists the media types the formatter can read into a model type, which <see cref="HttpRestClient"/> uses to build the <c>Accept</c> header, and
    /// <see cref="CanRead"/> and <see cref="ReadAsync"/> select the formatter for a response and read its content. The writing side turns objects into
    /// request content: <see cref="GetWritableMediaTypes"/> and <see cref="CanWrite"/> select the formatter for a payload, and <see cref="Write"/> creates
    /// the content.
    /// </para>
    /// <para>
    /// A formatter can support one side only. A receive-only formatter returns no writable media types and never accepts a payload, and a send-only
    /// formatter returns no readable media types and never accepts a response.
    /// </para>
    /// <para>
    /// Media types are case-insensitive, so implementations compare them ignoring case. Implementations are expected to be thread-safe, because a
    /// formatter registered with a client is shared by all its requests.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ContentFormatters"/>
    public interface IHttpContentFormatter
    {
        /// <summary>
        /// Returns the media types that this formatter can read into the specified model type.
        /// </summary>
        /// <param name="modelType">The type of the object to read.</param>
        /// <returns>The media types that this formatter can read into <paramref name="modelType"/>, in order of preference; empty if there are none.</returns>
        IEnumerable<string> GetReadableMediaTypes(Type modelType);

        /// <summary>
        /// Determines whether this formatter can read content of the specified media type into the specified model type.
        /// </summary>
        /// <param name="mediaType">The media type of the content.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <returns><see langword="true"/> if this formatter can read the content; otherwise, <see langword="false"/>.</returns>
        bool CanRead(string mediaType, Type modelType);

        /// <summary>
        /// Asynchronously reads an object of the specified type from HTTP content.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to read.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <param name="cancellationToken">A token for canceling the operation (optional).</param>
        /// <returns>A task that resolves to the object read from <paramref name="content"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> or <paramref name="modelType"/> is <see langword="null"/>.</exception>
        Task<object?> ReadAsync(HttpContent content, Type modelType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the media types in which this formatter can write a payload of the specified type.
        /// </summary>
        /// <param name="payloadType">The type of the payload to write.</param>
        /// <returns>The media types in which this formatter can write <paramref name="payloadType"/>, in order of preference; empty if there are none.</returns>
        IEnumerable<string> GetWritableMediaTypes(Type payloadType);

        /// <summary>
        /// Determines whether this formatter can write a payload of the specified type in the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <param name="payloadType">The type of the payload to write.</param>
        /// <returns><see langword="true"/> if this formatter can write the payload; otherwise, <see langword="false"/>.</returns>
        bool CanWrite(string mediaType, Type payloadType);

        /// <summary>
        /// Creates HTTP content that carries the specified payload in the specified media type.
        /// </summary>
        /// <param name="payload">The object to write.</param>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <returns>The <see cref="HttpContent"/> that carries <paramref name="payload"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="payload"/> or <paramref name="mediaType"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown if this formatter cannot write <paramref name="payload"/> in <paramref name="mediaType"/>.</exception>
        HttpContent Write(object payload, string mediaType);
    }
}
