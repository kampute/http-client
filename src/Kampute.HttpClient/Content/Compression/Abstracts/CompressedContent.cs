namespace Kampute.HttpClient.Content.Compression.Abstracts
{
    using Kampute.HttpClient.Content.Abstracts;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a base class for content that compresses another <see cref="HttpContent"/> as it is sent.
    /// </summary>
    /// <remarks>
    /// The content adds its encoding to the <c>Content-Encoding</c> header, and its length is not known until it is sent.
    /// </remarks>
    public abstract class CompressedContent : HttpContentDecorator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedContent"/> class.
        /// </summary>
        /// <param name="content">The content to compress. It is disposed when this instance is disposed.</param>
        /// <param name="contentEncoding">The value added to the <c>Content-Encoding</c> header, such as <c>gzip</c> or <c>deflate</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="contentEncoding"/> is <see langword="null"/> or empty.</exception>
        protected CompressedContent(HttpContent content, string contentEncoding)
            : base(content)
        {
            if (string.IsNullOrEmpty(contentEncoding))
                throw new ArgumentException("Content encoding cannot be null or empty.", nameof(contentEncoding));

            Headers.ContentEncoding.Add(contentEncoding);
        }

        /// <summary>
        /// When overridden in a derived class, returns a stream that compresses the data written to it into the specified stream.
        /// </summary>
        /// <param name="stream">The stream that receives the compressed data.</param>
        /// <returns>A <see cref="Stream"/> that compresses the data written to it into <paramref name="stream"/>.</returns>
        protected abstract Stream CompressStream(Stream stream);

        /// <summary>
        /// Writes the compressed content to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="context">The transport context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected sealed override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            using var compressionStream = CompressStream(stream);
            await OriginalContent.CopyToAsync(compressionStream).ConfigureAwait(false);
        }

        /// <summary>
        /// Indicates that the length of the compressed content is not known before it is sent.
        /// </summary>
        /// <param name="length">Always -1.</param>
        /// <returns>Always <see langword="false"/>.</returns>
        protected sealed override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
