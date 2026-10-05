namespace Kampute.HttpClient.Content
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents content with no body, whose length is zero.
    /// </summary>
    /// <remarks>
    /// Use it to send a request that needs content, such as one with <c>Content-*</c> headers, but no body.
    /// </remarks>
    public sealed class EmptyContent : HttpContent
    {
        /// <summary>
        /// Writes nothing to the stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="context">The transport context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the length of the content, which is zero.
        /// </summary>
        /// <param name="length">Always 0.</param>
        /// <returns>Always <see langword="true"/>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }
    }
}
