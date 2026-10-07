namespace Kampute.HttpClient.Test.Xml
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.TestSupport;
    using Kampute.HttpClient.Xml;
    using Kampute.Resilience;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using static Kampute.HttpClient.TestSupport.CompressedContentHelpers;

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

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, false);
            _restClient = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com/xml"),
            };
        }

        [TearDown]
        public void Cleanup()
        {
            _restClient.Dispose();
        }

        [Test]
        public void UseXml_RegistersOneFormatterAndConfiguresIt()
        {
            var first = _restClient.UseXml();
            var second = _restClient.UseXml(formatter => formatter.Serializer = XmlSerializerKind.DataContractSerializer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(second, Is.SameAs(first));
                Assert.That(second.Serializer, Is.EqualTo(XmlSerializerKind.DataContractSerializer));
                Assert.That(_restClient.ContentFormatters.OfType<XmlFormatter>().Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task UseXml_AdvertisesXmlInAcceptHeader()
        {
            _restClient.UseXml();
            var accepted = default(string);
            _mockMessageHandler.MockHttpResponse(request =>
            {
                accepted = request.Headers.Accept.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(new PlainModel { Name = "Test" }.ToXmlSerializerString(Encoding.UTF8), Encoding.UTF8, MediaTypeNames.Application.Xml),
                };
            });

            var result = await _restClient.GetAsync<PlainModel>("/model");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted, Is.EqualTo($"{MediaTypeNames.Application.Xml}, {MediaTypeNames.Text.Xml}, {MediaTypeNames.Application.ProblemXml}"));
                Assert.That(result, Is.EqualTo(new PlainModel { Name = "Test" }));
            }
        }

        [TestCase(XmlSerializerKind.XmlSerializer)]
        [TestCase(XmlSerializerKind.DataContractSerializer)]
        public async Task PostAsXmlAsync_InvokesHttpClientCorrectly(XmlSerializerKind serializer)
        {
            await AssertEchoed(HttpMethod.Post, serializer, (uri, payload) => _restClient.PostAsXmlAsync<ContractModel>(uri, payload));
        }

        [TestCase(XmlSerializerKind.XmlSerializer)]
        [TestCase(XmlSerializerKind.DataContractSerializer)]
        public async Task PutAsXmlAsync_InvokesHttpClientCorrectly(XmlSerializerKind serializer)
        {
            await AssertEchoed(HttpMethod.Put, serializer, (uri, payload) => _restClient.PutAsXmlAsync<ContractModel>(uri, payload));
        }

        [TestCase(XmlSerializerKind.XmlSerializer)]
        [TestCase(XmlSerializerKind.DataContractSerializer)]
        public async Task PatchAsXmlAsync_InvokesHttpClientCorrectly(XmlSerializerKind serializer)
        {
            await AssertEchoed(HttpMethod.Patch, serializer, (uri, payload) => _restClient.PatchAsXmlAsync<ContractModel>(uri, payload));
        }

        [Test]
        public async Task PostAsXmlAsync_WithoutRegistration_SendsWithDefaultSettings()
        {
            var sentBody = default(string);
            _mockMessageHandler.MockHttpResponse(request =>
            {
                sentBody = request.Content!.ReadAsStringAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            await _restClient.PostAsXmlAsync("/models", new ContractModel { Name = "Test" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sentBody, Is.EqualTo(new ContractModel { Name = "Test" }.ToDataContractString(Encoding.UTF8)));
                Assert.That(_restClient.ContentFormatters, Is.Empty);
            }
        }

        [Test]
        public void PostAsXmlAsync_WithNullPayload_ThrowsBeforeReturningTask()
        {
            Assert.Throws<ArgumentNullException>(() => _restClient.PostAsXmlAsync("/models", null!));
        }

        [TestCase("gzip", SocketError.HostUnreachable)]
        [TestCase("gzip", SocketError.TimedOut)]
        [TestCase("deflate", SocketError.HostUnreachable)]
        [TestCase("deflate", SocketError.TimedOut)]
        public async Task SendAsync_OnConnectionFailure_WithCompressedXmlContent_RetriesSerializedPayload(string encoding, SocketError socketError)
        {
            var payload = new PlainModel { Name = "XML Test" };
            var maxRetries = 2;
            var attempts = 0;

            _restClient.RetryPolicy = RetryStrategies.Constant(TimeSpan.Zero).WithMaxRetries((uint)maxRetries).ToHttpRetryPolicy();

            _mockMessageHandler.MockHttpResponse(request =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToXmlSerializerString(Encoding.UTF8)));
                }

                if (attempts <= maxRetries)
                    throw new HttpRequestException("Connection failure", new SocketException((int)socketError));

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            using var content = new XmlContent(payload);
            using var compressedContent = CompressContent(content, encoding);

            using var response = await _restClient.SendAsync(HttpMethod.Post, "/resource", compressedContent);

            Assert.That(attempts, Is.EqualTo(maxRetries + 1));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public void SendAsync_OnCallerCancellation_WithCompressedXmlContent_DoesNotRetry(string encoding)
        {
            var payload = new PlainModel { Name = "XML Test" };
            var attempts = 0;
            using var cancellationTokenSource = new CancellationTokenSource();

            _restClient.RetryPolicy = RetryStrategies.Constant(TimeSpan.Zero).WithMaxRetries(2).ToHttpRetryPolicy();

            _mockMessageHandler.MockHttpResponse((request, cancellationToken) =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToXmlSerializerString(Encoding.UTF8)));
                }

                cancellationTokenSource.Cancel();
                throw new OperationCanceledException(cancellationToken);
            });

            using var content = new XmlContent(payload);
            using var compressedContent = CompressContent(content, encoding);

            Assert.ThrowsAsync
            (
                Is.InstanceOf<OperationCanceledException>(),
                async () => await _restClient.SendAsync(HttpMethod.Post, "/resource", compressedContent, cancellationToken: cancellationTokenSource.Token)
            );
            Assert.That(attempts, Is.EqualTo(1));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public async Task SendAsync_OnTimeoutCancellation_WithCompressedXmlContent_UsesRetryPolicy(string encoding)
        {
            var payload = new PlainModel { Name = "XML Test" };
            var mockRetryPolicy = RetryTestHelpers.MockRetryPolicy(1, out var mockRetrySession);

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
                        Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToXmlSerializerString(Encoding.UTF8)));
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
                BaseAddress = new Uri("http://api.test.com/xml"),
            };
            timedOutClient.UseXml();
            timedOutClient.RetryPolicy = mockRetryPolicy.Object;

            using var content = new XmlContent(payload);
            using var compressedContent = CompressContent(content, encoding);

            using var response = await timedOutClient.SendAsync(HttpMethod.Post, "/resource", compressedContent);

            mockRetryPolicy.Verify(strategy => strategy.CreateSession(It.IsAny<HttpRequestErrorContext>()), Times.Once);
            mockRetrySession.Verify(session => session.WaitToRetryAsync(It.IsAny<CancellationToken>()), Times.Once);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(attempts, Is.EqualTo(2));
            }
        }

        private async Task AssertEchoed(HttpMethod method, XmlSerializerKind serializer, Func<string, ContractModel, Task<ContractModel?>> send)
        {
            _restClient.UseXml(formatter => formatter.Serializer = serializer);
            var payload = new ContractModel { Name = "XML Test" };
            var expectedBody = serializer == XmlSerializerKind.XmlSerializer
                ? payload.ToXmlSerializerString(Encoding.UTF8)
                : payload.ToDataContractString(Encoding.UTF8);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(method));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo(expectedBody));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await send("/echo", payload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.SameAs(payload));
                Assert.That(result, Is.EqualTo(payload));
            }
        }
    }
}
