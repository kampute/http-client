namespace Kampute.HttpClient.Json.Test
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using static Kampute.HttpClient.TestSupport.CompressedContentHelpers;

    [TestFixture]
    public class HttpRestClientJsonExtensionsTests
    {
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _restClient;

        private Uri AbsoluteUrl(string url)
        {
            return _restClient.BaseAddress is not null
                ? new Uri(_restClient.BaseAddress, url)
                : new Uri(url);
        }

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, false);
            _restClient = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com/json"),
            };
            _restClient.AcceptJson(TestModel.JsonOption);
            _restClient.SetJsonSerializerOptions(TestModel.JsonOption);
        }

        [TearDown]
        public void Cleanup()
        {
            _restClient.Dispose();
        }

        [Test]
        public async Task PostAsJsonAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "JSON Test" };

            _mockMessageHandler.MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PostAsJsonAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [Test]
        public async Task PutAsJsonAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "JSON Test" };

            _mockMessageHandler.MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PutAsJsonAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [Test]
        public async Task PatchAsJsonAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "JSON Test" };

            _mockMessageHandler.MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Patch));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PatchAsJsonAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [TestCase("gzip", SocketError.HostUnreachable)]
        [TestCase("gzip", SocketError.TimedOut)]
        [TestCase("deflate", SocketError.HostUnreachable)]
        [TestCase("deflate", SocketError.TimedOut)]
        public async Task SendAsync_OnConnectionFailure_WithCompressedJsonContent_RetriesSerializedPayload(string encoding, SocketError socketError)
        {
            var payload = new TestModel { Name = "JSON Test" };
            var maxRetries = 2;
            var attempts = 0;

            _restClient.BackoffStrategy = BackoffStrategies.Uniform((uint)maxRetries, TimeSpan.Zero);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToJsonString()));
                }

                if (attempts <= maxRetries)
                    throw new HttpRequestException("Connection failure", new SocketException((int)socketError));

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            using var content = new JsonContent(payload)
            {
                Options = TestModel.JsonOption
            };
            using var compressedContent = CompressContent(content, encoding);

            using var response = await _restClient.SendAsync(HttpMethod.Post, "/resource", compressedContent);

            Assert.That(attempts, Is.EqualTo(maxRetries + 1));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public void SendAsync_OnCallerCancellation_WithCompressedJsonContent_DoesNotRetry(string encoding)
        {
            var payload = new TestModel { Name = "JSON Test" };
            var attempts = 0;
            using var cancellationTokenSource = new CancellationTokenSource();

            _restClient.BackoffStrategy = BackoffStrategies.Uniform(2, TimeSpan.Zero);

            _mockMessageHandler.MockHttpResponse((request, cancellationToken) =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToJsonString()));
                }

                cancellationTokenSource.Cancel();
                throw new OperationCanceledException(cancellationToken);
            });

            using var content = new JsonContent(payload)
            {
                Options = TestModel.JsonOption
            };
            using var compressedContent = CompressContent(content, encoding);

            Assert.ThrowsAsync
            (
                Is.InstanceOf<OperationCanceledException>(),
                async () => await _restClient.SendAsync(HttpMethod.Post, "/resource", compressedContent, cancellationTokenSource.Token)
            );
            Assert.That(attempts, Is.EqualTo(1));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public async Task SendAsync_OnTimeoutCancellation_WithCompressedJsonContent_UsesBackoffStrategy(string encoding)
        {
            var payload = new TestModel { Name = "JSON Test" };
            var mockBackoffStrategy = RetryTestHelpers.MockBackoffStrategy(1, out var mockRetryScheduler);

            var attempts = 0;
            using var testHandler = new TestHttpMessageHandler
            {
                ResponseFactory = async (request, cancellationToken) =>
                {
                    ++attempts;

                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(request.Content, Is.Not.Null);
                        Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                        Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToJsonString()));
                    }

                    if (attempts == 1)
                        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }
            };
            using var timedOutHttpClient = new HttpClient(testHandler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            };

            using var timedOutClient = new HttpRestClient(timedOutHttpClient)
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
            timedOutClient.AcceptJson();
            timedOutClient.BackoffStrategy = mockBackoffStrategy.Object;

            using var content = new JsonContent(payload)
            {
                Options = TestModel.JsonOption
            };
            using var compressedContent = CompressContent(content, encoding);

            using var response = await timedOutClient.SendAsync(HttpMethod.Post, "/resource", compressedContent);

            mockBackoffStrategy.Verify(strategy => strategy.CreateScheduler(It.IsAny<HttpRequestErrorContext>()), Times.Once);
            mockRetryScheduler.Verify(scheduler => scheduler.WaitAsync(It.IsAny<CancellationToken>()), Times.Once);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(attempts, Is.EqualTo(2));
            }
        }
    }
}
