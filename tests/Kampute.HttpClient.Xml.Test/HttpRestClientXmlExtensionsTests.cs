namespace Kampute.HttpClient.Xml.Test
{
    using Kampute.HttpClient;
    using Moq;
    using Moq.Protected;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpRestClientXmlExtensionsTests
    {
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _restClient;

        private Uri AbsoluteUrl(string url)
        {
            return _restClient.BaseAddress is not null
                ? new Uri(_restClient.BaseAddress, url)
                : new Uri(url);
        }

        private void MockHttpResponse(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            MockHttpResponse((request, _) => responseFactory(request));
        }

        private void MockHttpResponse(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory)
        {
            _mockMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>
                (
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync
                (
                    (HttpRequestMessage request, CancellationToken cancellationToken) => responseFactory(request, cancellationToken)
                );
        }

        private static string ReadCompressedContent(HttpContent content)
        {
            using var compressedStream = new MemoryStream();
            content.CopyToAsync(compressedStream).GetAwaiter().GetResult();
            compressedStream.Position = 0;

            using Stream decompressedStream = content.Headers.ContentEncoding.ToString() switch
            {
                "gzip" => new GZipStream(compressedStream, CompressionMode.Decompress),
                "deflate" => new DeflateStream(compressedStream, CompressionMode.Decompress),
                _ => throw new InvalidOperationException("Unsupported encoding")
            };
            using var reader = new StreamReader(decompressedStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static HttpContent CompressContent(HttpContent content, string encoding)
        {
            return encoding switch
            {
                "gzip" => content.AsGzip(),
                "deflate" => content.AsDeflate(),
                _ => throw new InvalidOperationException("Unsupported encoding")
            };
        }

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, false);
            _restClient = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com/xml"),
            };
            _restClient.AcceptXml();
        }

        [TearDown]
        public void Cleanup()
        {
            _restClient.Dispose();
        }

        [Test]
        public async Task PostAsXmlAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "XML Test" };

            MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PostAsXmlAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [Test]
        public async Task PutAsXmlAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "XML Test" };

            MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PutAsXmlAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [Test]
        public async Task PatchAsXmlAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "XML Test" };

            MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Patch));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PatchAsXmlAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [TestCase("gzip", SocketError.HostUnreachable)]
        [TestCase("gzip", SocketError.TimedOut)]
        [TestCase("deflate", SocketError.HostUnreachable)]
        [TestCase("deflate", SocketError.TimedOut)]
        public async Task SendAsync_OnConnectionFailure_WithCompressedXmlContent_RetriesSerializedPayload(string encoding, SocketError socketError)
        {
            var payload = new TestModel { Name = "XML Test" };
            var maxRetries = 2;
            var attempts = 0;

            _restClient.BackoffStrategy = BackoffStrategies.Uniform((uint)maxRetries, TimeSpan.Zero);

            MockHttpResponse(request =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToXmlString(Encoding.UTF8)));
                }

                if (attempts <= maxRetries)
                    throw new HttpRequestException("Connection failure", new SocketException((int)socketError));

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            using var content = new XmlContent(payload);
            using var compressedContent = CompressContent(content, encoding);

            using var response = await _restClient.SendAsync(HttpMethod.Post, "/echo", compressedContent);

            Assert.That(attempts, Is.EqualTo(maxRetries + 1));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public void SendAsync_OnCallerCancellation_WithCompressedXmlContent_DoesNotRetry(string encoding)
        {
            var payload = new TestModel { Name = "XML Test" };
            var attempts = 0;
            using var cancellationTokenSource = new CancellationTokenSource();

            _restClient.BackoffStrategy = BackoffStrategies.Uniform(2, TimeSpan.Zero);

            MockHttpResponse((request, cancellationToken) =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToXmlString(Encoding.UTF8)));
                }

                cancellationTokenSource.Cancel();
                throw new OperationCanceledException(cancellationToken);
            });

            using var content = new XmlContent(payload);
            using var compressedContent = CompressContent(content, encoding);

            Assert.ThrowsAsync
            (
                Is.InstanceOf<OperationCanceledException>(),
                async () => await _restClient.SendAsync(HttpMethod.Post, "/echo", compressedContent, cancellationTokenSource.Token)
            );
            Assert.That(attempts, Is.EqualTo(1));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public async Task SendAsync_OnTaskCanceledTimeout_WithCompressedXmlContent_RetriesSerializedPayload(string encoding)
        {
            var payload = new TestModel { Name = "XML Test" };
            var maxRetries = 2;
            var attempts = 0;

            _restClient.BackoffStrategy = BackoffStrategies.Uniform((uint)maxRetries, TimeSpan.Zero);

            MockHttpResponse(request =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToXmlString(Encoding.UTF8)));
                }

                if (attempts <= maxRetries)
                    throw new TaskCanceledException("The request timed out.");

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            using var content = new XmlContent(payload);
            using var compressedContent = CompressContent(content, encoding);

            using var response = await _restClient.SendAsync(HttpMethod.Post, "/echo", compressedContent);

            Assert.That(attempts, Is.EqualTo(maxRetries + 1));
        }
    }
}
