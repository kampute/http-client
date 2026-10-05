// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Content.Abstracts;
    using Kampute.HttpClient.Content.Compression;
    using System;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// Provides extension methods for <see cref="HttpContent"/>.
    /// </summary>
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Returns the character encoding named by the <c>charset</c> parameter of the <c>Content-Type</c> header.
        /// </summary>
        /// <param name="httpContent">The content whose encoding to return.</param>
        /// <returns>The <see cref="Encoding"/> that the header names, or <see langword="null"/> if the header names none.</returns>
        /// <exception cref="ArgumentException">Thrown if the header names a character set that the runtime does not support.</exception>
        public static Encoding? FindCharacterEncoding(this HttpContent httpContent)
        {
            return httpContent.Headers.ContentType?.CharSet is string charSet ? Encoding.GetEncoding(charSet) : null;
        }

        /// <summary>
        /// Determines whether the content can be sent more than once, as a retry requires.
        /// </summary>
        /// <param name="httpContent">The content to check.</param>
        /// <returns><see langword="true"/> if the content can be sent again; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// A <see cref="StreamContent"/> can be sent again only if its length is known, which is the case for a seekable stream; a non-seekable
        /// stream can be read only once. Content that wraps another content, such as compressed content, can be sent again if the wrapped content can.
        /// Other content is assumed to be reusable.
        /// </remarks>
        public static bool IsReusable(this HttpContent httpContent)
        {
            return httpContent switch
            {
                null => true,
                HttpContentDecorator decorator => decorator.OriginalContent.IsReusable(),
                StreamContent streamContent => streamContent.Headers.ContentLength.HasValue,
                _ => true,
            };
        }

        /// <summary>
        /// Wraps the content in content that compresses it with GZip as it is sent.
        /// </summary>
        /// <param name="httpContent">The content to compress. It is disposed when the returned content is disposed.</param>
        /// <param name="compressionLevel">Whether to favor speed or size. The default is <see cref="CompressionLevel.Optimal"/>.</param>
        /// <returns>A <see cref="GzipCompressedContent"/> that wraps <paramref name="httpContent"/>.</returns>
        public static GzipCompressedContent AsGzip(this HttpContent httpContent, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            return new GzipCompressedContent(httpContent, compressionLevel);
        }

        /// <summary>
        /// Wraps the content in content that compresses it with Deflate as it is sent.
        /// </summary>
        /// <param name="httpContent">The content to compress. It is disposed when the returned content is disposed.</param>
        /// <param name="compressionLevel">Whether to favor speed or size. The default is <see cref="CompressionLevel.Optimal"/>.</param>
        /// <returns>A <see cref="DeflateCompressedContent"/> that wraps <paramref name="httpContent"/>.</returns>
        public static DeflateCompressedContent AsDeflate(this HttpContent httpContent, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            return new DeflateCompressedContent(httpContent, compressionLevel);
        }
    }
}
