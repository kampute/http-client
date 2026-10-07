// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Content.Abstracts
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a base class for content formatters that read and write a fixed set of media types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A derived class passes the media types it reads and the media types it writes to the constructor. A receive-only formatter passes no writable
    /// media types and overrides <see cref="ReadContentAsync"/>; a send-only formatter passes no readable media types and overrides <see cref="CreateContent"/>;
    /// a two-way formatter does both. To limit the types a formatter handles, override <see cref="CanReadType"/> or <see cref="CanWriteType"/>. To read
    /// media types that are not listed, such as every media type with a structured syntax suffix, override <see cref="CanReadMediaType"/>.
    /// </para>
    /// <para>
    /// Media types are compared ignoring case. <see cref="ReadAsync"/> and <see cref="Write"/> validate their arguments before they call the
    /// overridable members.
    /// </para>
    /// </remarks>
    public abstract class HttpContentFormatter : IHttpContentFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentFormatter"/> class with the media types it reads and writes.
        /// </summary>
        /// <param name="readableMediaTypes">The media types this formatter reads, in order of preference; empty for a send-only formatter.</param>
        /// <param name="writableMediaTypes">The media types this formatter writes, in order of preference; empty for a receive-only formatter.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="readableMediaTypes"/> or <paramref name="writableMediaTypes"/> is <see langword="null"/>.</exception>
        protected HttpContentFormatter(IEnumerable<string> readableMediaTypes, IEnumerable<string> writableMediaTypes)
        {
            if (readableMediaTypes is null)
                throw new ArgumentNullException(nameof(readableMediaTypes));
            if (writableMediaTypes is null)
                throw new ArgumentNullException(nameof(writableMediaTypes));

            ReadableMediaTypes = readableMediaTypes.ToArray();
            WritableMediaTypes = writableMediaTypes.ToArray();
        }

        /// <summary>
        /// Gets the media types this formatter reads.
        /// </summary>
        /// <value>
        /// The media types this formatter reads, in order of preference.
        /// </value>
        public IReadOnlyCollection<string> ReadableMediaTypes { get; }

        /// <summary>
        /// Gets the media types this formatter writes.
        /// </summary>
        /// <value>
        /// The media types this formatter writes, in order of preference.
        /// </value>
        public IReadOnlyCollection<string> WritableMediaTypes { get; }

        /// <summary>
        /// Returns the media types that this formatter can read into the specified model type.
        /// </summary>
        /// <param name="modelType">The type of the object to read.</param>
        /// <returns>
        /// <see cref="ReadableMediaTypes"/> if <see cref="CanReadType"/> accepts <paramref name="modelType"/>; otherwise, an empty collection.
        /// </returns>
        public virtual IEnumerable<string> GetReadableMediaTypes(Type modelType)
        {
            return modelType is not null && ReadableMediaTypes.Count != 0 && CanReadType(modelType) ? ReadableMediaTypes : [];
        }

        /// <summary>
        /// Determines whether this formatter can read content of the specified media type into the specified model type.
        /// </summary>
        /// <param name="mediaType">The media type of the content.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <returns>
        /// <see langword="true"/> if <see cref="CanReadMediaType"/> accepts <paramref name="mediaType"/> and <see cref="CanReadType"/> accepts
        /// <paramref name="modelType"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool CanRead(string mediaType, Type modelType)
        {
            return mediaType is not null && modelType is not null
                && CanReadMediaType(mediaType)
                && CanReadType(modelType);
        }

        /// <summary>
        /// Asynchronously reads an object of the specified type from HTTP content.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to read.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <param name="cancellationToken">A token for canceling the operation (optional).</param>
        /// <returns>A task that resolves to the object read from <paramref name="content"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> or <paramref name="modelType"/> is <see langword="null"/>.</exception>
        public Task<object?> ReadAsync(HttpContent content, Type modelType, CancellationToken cancellationToken = default)
        {
            if (content is null)
                throw new ArgumentNullException(nameof(content));
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            return ReadContentAsync(content, modelType, cancellationToken);
        }

        /// <summary>
        /// Returns the media types in which this formatter can write a payload of the specified type.
        /// </summary>
        /// <param name="payloadType">The type of the payload to write.</param>
        /// <returns>
        /// <see cref="WritableMediaTypes"/> if <see cref="CanWriteType"/> accepts <paramref name="payloadType"/>; otherwise, an empty collection.
        /// </returns>
        public virtual IEnumerable<string> GetWritableMediaTypes(Type payloadType)
        {
            return payloadType is not null && WritableMediaTypes.Count != 0 && CanWriteType(payloadType) ? WritableMediaTypes : [];
        }

        /// <summary>
        /// Determines whether this formatter can write a payload of the specified type in the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <param name="payloadType">The type of the payload to write.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="mediaType"/> is one of the <see cref="WritableMediaTypes"/> and <see cref="CanWriteType"/> accepts
        /// <paramref name="payloadType"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool CanWrite(string mediaType, Type payloadType)
        {
            return mediaType is not null && payloadType is not null
                && WritableMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase)
                && CanWriteType(payloadType);
        }

        /// <summary>
        /// Creates HTTP content that carries the specified payload in the specified media type.
        /// </summary>
        /// <param name="payload">The object to write.</param>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <returns>The <see cref="HttpContent"/> that carries <paramref name="payload"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="payload"/> or <paramref name="mediaType"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown if <see cref="CanWrite"/> returns <see langword="false"/> for <paramref name="mediaType"/> and the type of <paramref name="payload"/>.</exception>
        public HttpContent Write(object payload, string mediaType)
        {
            if (payload is null)
                throw new ArgumentNullException(nameof(payload));
            if (mediaType is null)
                throw new ArgumentNullException(nameof(mediaType));
            if (!CanWrite(mediaType, payload.GetType()))
                throw new NotSupportedException($"{GetType().Name} cannot write an object of type '{payload.GetType()}' as '{mediaType}'.");

            return CreateContent(payload, mediaType);
        }

        /// <summary>
        /// Determines whether this formatter can read objects of the specified type.
        /// </summary>
        /// <param name="modelType">The type of the object to read.</param>
        /// <returns><see langword="true"/> if this formatter can read objects of <paramref name="modelType"/>; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</returns>
        protected virtual bool CanReadType(Type modelType) => true;

        /// <summary>
        /// Determines whether this formatter can read content of the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type of the content, without parameters.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="mediaType"/> is one of the <see cref="ReadableMediaTypes"/>, ignoring case; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <see cref="CanRead"/> calls this method with a non-null media type. A media type accepted only by an override is read but not advertised
        /// in the <c>Accept</c> header, which lists <see cref="ReadableMediaTypes"/>.
        /// </remarks>
        protected virtual bool CanReadMediaType(string mediaType) => ReadableMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether a media type ends with the specified structured syntax suffix.
        /// </summary>
        /// <param name="mediaType">The media type to check, such as <c>application/vnd.example+json</c>.</param>
        /// <param name="suffix">The suffix, including its plus sign, such as <c>+json</c>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="mediaType"/> has a subtype name before <paramref name="suffix"/> and ends with it, ignoring case;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="mediaType"/> or <paramref name="suffix"/> is <see langword="null"/>.</exception>
        protected static bool HasStructuredSyntaxSuffix(string mediaType, string suffix)
        {
            if (mediaType is null)
                throw new ArgumentNullException(nameof(mediaType));
            if (suffix is null)
                throw new ArgumentNullException(nameof(suffix));

            var slash = mediaType.IndexOf('/');
            return slash > 0
                && mediaType.Length - suffix.Length > slash + 1
                && mediaType.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether this formatter can write payloads of the specified type.
        /// </summary>
        /// <param name="payloadType">The type of the payload to write.</param>
        /// <returns><see langword="true"/> if this formatter can write payloads of <paramref name="payloadType"/>; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</returns>
        protected virtual bool CanWriteType(Type payloadType) => true;

        /// <summary>
        /// When overridden in a derived class, asynchronously reads an object of the specified type from HTTP content.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to read.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to the object read from <paramref name="content"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown by the base implementation, for a formatter that does not read content.</exception>
        /// <remarks>
        /// <see cref="ReadAsync"/> calls this method after it has validated its arguments.
        /// </remarks>
        protected virtual Task<object?> ReadContentAsync(HttpContent content, Type modelType, CancellationToken cancellationToken)
        {
            throw new NotSupportedException($"{GetType().Name} does not read content.");
        }

        /// <summary>
        /// When overridden in a derived class, creates HTTP content that carries the specified payload in the specified media type.
        /// </summary>
        /// <param name="payload">The object to write.</param>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <returns>The <see cref="HttpContent"/> that carries <paramref name="payload"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown by the base implementation, for a formatter that does not write content.</exception>
        /// <remarks>
        /// <see cref="Write"/> calls this method after it has validated its arguments and checked them with <see cref="CanWrite"/>.
        /// </remarks>
        protected virtual HttpContent CreateContent(object payload, string mediaType)
        {
            throw new NotSupportedException($"{GetType().Name} does not write content.");
        }
    }
}
