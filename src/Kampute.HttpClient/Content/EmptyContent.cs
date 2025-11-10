namespace Kampute.HttpClient.Content
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an HTTP content with no data.
    /// </summary>
    /// <remarks>
    /// This class is used when an HTTP request or response needs to indicate a content body, but there should be no actual content sent or received.
    /// It effectively sets the content length to 0 and does not write anything to the output stream.
    /// </remarks>
    public sealed class EmptyContent : HttpContent
    {
        /// <summary>
        /// Serializes the content to a stream asynchronously.
        /// </summary>
        /// <param name="stream">The target stream to which the content should be written.</param>
        /// <param name="context">The transport context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Attempts to compute the length of the content.
        /// </summary>
        /// <param name="length">When this method returns, contains the length of the content in bytes.</param>
        /// <returns><see langword="true"/> if the length could be computed; otherwise, <see langword="false"/>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }
    }
}
