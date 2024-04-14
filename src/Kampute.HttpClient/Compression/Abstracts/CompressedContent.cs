namespace Kampute.HttpClient.Compression.Abstracts
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a base class for creating HTTP content based on compression.
    /// </summary>
    public abstract class CompressedContent : HttpContent
    {
        private readonly HttpContent _originalContent;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedContent"/> class.
        /// </summary>
        /// <param name="contentEncoding">The encoding type used for compression (e.g., gzip, deflate).</param>
        /// <param name="content">The HTTP content to compress.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="contentEncoding"/> is <c>null</c> or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
        protected CompressedContent(string contentEncoding, HttpContent content)
        {
            if (string.IsNullOrEmpty(contentEncoding))
                throw new ArgumentException("Content encoding cannot be null or empty.", nameof(contentEncoding));

            _originalContent = content ?? throw new ArgumentNullException(nameof(content));

            foreach (var header in content.Headers)
                Headers.TryAddWithoutValidation(header.Key, header.Value);

            Headers.ContentEncoding.Add(contentEncoding);
        }

        /// <summary>
        /// When overridden in a derived class, returns a stream that wraps the provided base stream with a compression layer.
        /// </summary>
        /// <param name="baseStream">The original stream to wrap with a compression stream.</param>
        /// <returns>A <see cref="Stream"/> that compresses the content as it is written to the <paramref name="baseStream"/>.</returns>
        protected abstract Stream WrapWithCompressionStream(Stream baseStream);

        /// <summary>
        /// Serializes the HTTP content to a stream as an asynchronous operation.
        /// </summary>
        /// <param name="stream">The target stream to which the content will be written.</param>
        /// <param name="context">Information about the transport (e.g., channel binding token).</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected sealed override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using var compressionStream = WrapWithCompressionStream(stream);
            await _originalContent.CopyToAsync(compressionStream);
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

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="HttpContent"/> and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _originalContent.Dispose();

            base.Dispose(disposing);
        }
    }
}
