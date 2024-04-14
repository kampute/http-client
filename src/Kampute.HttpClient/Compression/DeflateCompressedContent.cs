namespace Kampute.HttpClient.Compression
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using Kampute.HttpClient.Compression.Abstracts;

    /// <summary>
    /// Provides an HTTP content encapsulation that compresses the underlying content using the Deflate compression algorithm.
    /// </summary>
    public sealed class DeflateCompressedContent : CompressedContent
    {
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeflateCompressedContent"/> class.
        /// </summary>
        /// <param name="content">The content to compress using the Deflate compression algorithm.</param>
        /// <param name="compressionLevel">The level of compression that indicates whether to emphasize speed or compression efficiency.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
        public DeflateCompressedContent(HttpContent content, CompressionLevel compressionLevel = CompressionLevel.Fastest)
            : base("deflate", content)
        {
            _compressionLevel = compressionLevel;
        }

        /// <summary>
        /// Wraps the provided base stream with a Deflate compression stream.
        /// </summary>
        /// <param name="baseStream">The original stream to wrap with a Deflate compression stream.</param>
        /// <returns>A <see cref="Stream"/> that applies Deflate compression to the data written to the <paramref name="baseStream"/>.</returns>
        protected override Stream WrapWithCompressionStream(Stream baseStream)
        {
            return new DeflateStream(baseStream, _compressionLevel, leaveOpen: true);
        }
    }
}
