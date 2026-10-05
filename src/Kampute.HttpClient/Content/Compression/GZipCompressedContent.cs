namespace Kampute.HttpClient.Content.Compression
{
    using Kampute.HttpClient.Content.Compression.Abstracts;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;

    /// <summary>
    /// Represents content that compresses another <see cref="HttpContent"/> with GZip as it is sent.
    /// </summary>
    public sealed class GzipCompressedContent : CompressedContent
    {
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="GzipCompressedContent"/> class.
        /// </summary>
        /// <param name="content">The content to compress. It is disposed when this instance is disposed.</param>
        /// <param name="compressionLevel">Whether to favor speed or size.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
        public GzipCompressedContent(HttpContent content, CompressionLevel compressionLevel)
            : base(content, "gzip")
        {
            _compressionLevel = compressionLevel;
        }

        /// <summary>
        /// Returns a stream that compresses the data written to it with GZip into the specified stream.
        /// </summary>
        /// <param name="stream">The stream that receives the compressed data.</param>
        /// <returns>A <see cref="GZipStream"/> that writes to <paramref name="stream"/>.</returns>
        protected override Stream CompressStream(Stream stream)
        {
            return new GZipStream(stream, _compressionLevel, leaveOpen: true);
        }
    }
}
