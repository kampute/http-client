// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Content.Compression;
    using System;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// Provides extension methods for <see cref="HttpContent"/> to enhance functionality related to HTTP content processing.
    /// </summary>
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Attempts to find the character encoding from the <see cref="HttpContent"/> headers.
        /// </summary>
        /// <param name="httpContent">The <see cref="HttpContent"/> instance to extract the character encoding from.</param>
        /// <returns>The <see cref="Encoding"/> specified in the content's headers if the charset is recognized; otherwise, <c>null</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if the charset specified in the content's headers is not recognized.</exception>
        /// <remarks>
        /// This method inspects the 'CharSet' value in the content type header of the <see cref="HttpContent"/>. If the charset is specified
        /// and recognized, it returns the corresponding <see cref="Encoding"/>. If the charset is not specified, the method returns <c>null</c>, 
        /// indicating that the encoding could not be determined. An <see cref="ArgumentException"/> is thrown if the charset is specified but 
        /// not supported by the system.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Encoding? FindCharacterEncoding(this HttpContent httpContent)
        {
            return httpContent.Headers.ContentType?.CharSet is string charSet ? Encoding.GetEncoding(charSet) : null;
        }

        /// <summary>
        /// Compresses the <see cref="HttpContent"/> using the GZIP compression algorithm.
        /// </summary>
        /// <param name="httpContent">The HTTP content to compress.</param>
        /// <param name="compressionLevel">The level of compression that indicates whether to emphasize speed or compression efficiency.</param>
        /// <returns>A new instance of <see cref="GzipCompressedContent"/> that wraps the original HTTP content with GZIP compression.</returns>
        public static GzipCompressedContent AsGzip(this HttpContent httpContent, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            return new GzipCompressedContent(httpContent, compressionLevel);
        }

        /// <summary>
        /// Compresses the <see cref="HttpContent"/> using the Deflate compression algorithm.
        /// </summary>
        /// <param name="httpContent">The HTTP content to compress.</param>
        /// <param name="compressionLevel">The level of compression that indicates whether to emphasize speed or compression efficiency.</param>
        /// <returns>A new instance of <see cref="DeflateCompressedContent"/> that wraps the original HTTP content with Deflate compression.</returns>
        public static DeflateCompressedContent AsDeflate(this HttpContent httpContent, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            return new DeflateCompressedContent(httpContent, compressionLevel);
        }
    }
}
