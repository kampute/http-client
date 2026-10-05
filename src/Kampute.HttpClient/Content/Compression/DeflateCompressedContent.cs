namespace Kampute.HttpClient.Content.Compression
{
    using Kampute.HttpClient.Content.Compression.Abstracts;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;

    /// <summary>
    /// Represents content that compresses another <see cref="HttpContent"/> with Deflate as it is sent.
    /// </summary>
    public sealed class DeflateCompressedContent : CompressedContent
    {
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateCompressedContent"/> class.
        /// </summary>
        /// <param name="content">The content to compress. It is disposed when this instance is disposed.</param>
        /// <param name="compressionLevel">Whether to favor speed or size. The default is <see cref="CompressionLevel.Fastest"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
        public DeflateCompressedContent(HttpContent content, CompressionLevel compressionLevel = CompressionLevel.Fastest)
            : base(content, "deflate")
        {
            _compressionLevel = compressionLevel;
        }

        /// <summary>
        /// Returns a stream that compresses the data written to it with Deflate into the specified stream.
        /// </summary>
        /// <param name="stream">The stream that receives the compressed data.</param>
        /// <returns>A <see cref="DeflateStream"/> that writes to <paramref name="stream"/>.</returns>
        protected override Stream CompressStream(Stream stream)
        {
            return new DeflateStream(stream, _compressionLevel, leaveOpen: true);
        }
    }
}
