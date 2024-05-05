namespace Kampute.HttpClient.Content.Compression.Abstracts
{
    using Kampute.HttpClient.Content.Abstracts;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a base class for creating HTTP content based on compression.
    /// </summary>
    public abstract class CompressedContent : HttpContentDecorator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedContent"/> class.
        /// </summary>
        /// <param name="content">The HTTP content to compress.</param>
        /// <param name="contentEncoding">The encoding type used for compression (e.g., gzip, deflate).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="contentEncoding"/> is <c>null</c> or empty.</exception>
        protected CompressedContent(HttpContent content, string contentEncoding)
            : base(content)
        {
            if (string.IsNullOrEmpty(contentEncoding))
                throw new ArgumentException("Content encoding cannot be null or empty.", nameof(contentEncoding));

            Headers.ContentEncoding.Add(contentEncoding);
        }

        /// <summary>
        /// When overridden in a derived class, returns a stream that wraps the provided base stream with a compression layer.
        /// </summary>
        /// <param name="stream">The original stream to wrap with a compression stream.</param>
        /// <returns>A <see cref="Stream"/> that compresses the content as it is written to the <paramref name="stream"/>.</returns>
        protected abstract Stream CompressStream(Stream stream);

        /// <summary>
        /// Serializes the HTTP content to a stream as an asynchronous operation.
        /// </summary>
        /// <param name="stream">The target stream to which the content will be written.</param>
        /// <param name="context">Information about the transport (e.g., channel binding token).</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected sealed override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using var compressionStream = CompressStream(stream);
            await OriginalContent.CopyToAsync(compressionStream);
        }

        /// <summary>
        /// Tries to compute the length of the compressed content.
        /// </summary>
        /// <param name="length">The length of the content, if it can be computed.</param>
        /// <returns><c>false</c> as the compressed content length is not predictable before compression.</returns>
        protected sealed override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
