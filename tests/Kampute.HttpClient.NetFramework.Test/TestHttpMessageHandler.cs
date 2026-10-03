namespace Kampute.HttpClient.NetFramework.Test
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A message handler that answers each request with a delegate. It sits under a real <see cref="System.Net.Http.HttpClient"/>,
    /// so the .NET Framework client code that sends and disposes request content still runs.
    /// </summary>
    public sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
        }

        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ++Attempts;
            try
            {
                return Task.FromResult(_responseFactory(request));
            }
            catch (Exception error)
            {
                // A real handler reports failures through the returned task, not by throwing synchronously.
                return Task.FromException<HttpResponseMessage>(error);
            }
        }

        /// <summary>
        /// Reads the request body the way a real handler would when it sends the request.
        /// </summary>
        public static string ReadContent(HttpContent content)
        {
            if (content is null)
                throw new ArgumentNullException(nameof(content));

            using var buffer = new MemoryStream();
            content.CopyToAsync(buffer).GetAwaiter().GetResult();
            buffer.Position = 0;

            using Stream decoded = content.Headers.ContentEncoding.Count == 0 ? buffer : content.Headers.ContentEncoding.ToString() switch
            {
                "gzip" => new GZipStream(buffer, CompressionMode.Decompress),
                "deflate" => new DeflateStream(buffer, CompressionMode.Decompress),
                _ => throw new InvalidOperationException("Unsupported encoding")
            };
            using var reader = new StreamReader(decoded, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
