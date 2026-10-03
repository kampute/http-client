// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Content
{
    using Kampute.HttpClient.Content.Abstracts;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an HTTP content that sends another content unchanged, without taking ownership of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class passes the headers and body of the original content through unchanged. Disposing an instance of this class does not dispose
    /// the original content, so the original content stays usable and its owner remains responsible for disposing it.
    /// </para>
    /// <para>
    /// Use this class when a component that disposes the content it receives must not end the life of the original content. For example,
    /// <see cref="System.Net.Http.HttpClient"/> on .NET Framework disposes the content of every request it sends, which prevents the same
    /// content from being sent again. Sending a <see cref="NonOwningContent"/> instead keeps the original content available for another attempt.
    /// </para>
    /// <para>
    /// The original content can be sent again only if it is reusable. For example, a <see cref="StreamContent"/> over a stream that cannot be
    /// read twice remains non-reusable when wrapped.
    /// </para>
    /// </remarks>
    public sealed class NonOwningContent : HttpContentDecorator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonOwningContent"/> class.
        /// </summary>
        /// <param name="content">The HTTP content to send. It is not disposed when this instance is disposed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
        public NonOwningContent(HttpContent content)
            : base(content, leaveOpen: true)
        {
        }

        /// <summary>
        /// Serializes the original content to a stream as an asynchronous operation.
        /// </summary>
        /// <param name="stream">The target stream to which the content will be written.</param>
        /// <param name="context">Information about the transport (e.g., channel binding token).</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return OriginalContent.CopyToAsync(stream, context);
        }

        /// <summary>
        /// Tries to compute the length of the content.
        /// </summary>
        /// <param name="length">When this method returns, contains the length of the original content in bytes, if it is known.</param>
        /// <returns><see langword="true"/> if the length of the original content is known; otherwise, <see langword="false"/>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            var contentLength = OriginalContent.Headers.ContentLength;
            length = contentLength.GetValueOrDefault(-1);
            return contentLength.HasValue;
        }
    }
}
