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
        /// <inheritdoc/>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }
    }
}
