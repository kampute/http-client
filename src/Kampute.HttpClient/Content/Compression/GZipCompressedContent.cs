namespace Kampute.HttpClient.Content.Compression
{
    using Kampute.HttpClient.Content.Compression.Abstracts;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;

    /// <summary>
    /// Provides an HTTP content encapsulation that compresses the underlying content using the GZIP compression algorithm.
    /// </summary>
    public sealed class GzipCompressedContent : CompressedContent
    {
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="GzipCompressedContent"/> class.
        /// </summary>
        /// <param name="content">The content to compress using the GZIP compression algorithm.</param>
        /// <param name="compressionLevel">The level of compression that indicates whether to emphasize speed or compression efficiency.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
        public GzipCompressedContent(HttpContent content, CompressionLevel compressionLevel)
            : base(content, "gzip")
        {
            _compressionLevel = compressionLevel;
        }

        /// <summary>
        /// Wraps the provided base stream with a GZIP compression stream.
        /// </summary>
        /// <param name="baseStream">The original stream to wrap with a GZIP compression stream.</param>
        /// <returns>A <see cref="Stream"/> that applies GZIP compression to the data written to the <paramref name="baseStream"/>.</returns>
        protected override Stream WrapWithCompressionStream(Stream baseStream)
        {
            return new GZipStream(baseStream, _compressionLevel, leaveOpen: true);
        }
    }
}
