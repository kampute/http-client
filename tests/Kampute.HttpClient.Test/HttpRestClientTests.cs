namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpRestClientTests
    {
        private readonly TestContentDeserializer _testContentFormatter = new();
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _client;

        private Uri AbsoluteUrl(string url) => _client.BaseAddress is not null
            ? new Uri(_client.BaseAddress, url)
            : new Uri(url);

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, false);
            _client = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
            _client.ResponseDeserializers.Add(_testContentFormatter);
        }

        [TearDown]
        public void Cleanup()
        {
            _client.Dispose();
        }

        [Test]
        public async Task DefaultRequestHeaders_AreCopiedToOutgoingRequestHeaders()
        {
            var testerAgent = new ProductInfoHeaderValue("Tester", "1.0");
            _client.DefaultRequestHeaders.UserAgent.Add(testerAgent);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.That(request.Headers.UserAgent, Contains.Item(testerAgent));
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            await _client.SendAsync(HttpMethod.Options, "/resource");
        }

        [Test]
        public async Task SendAsync_NonGeneric_ReturnsResponseHeaders()
        {
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Head));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Headers.Add("X-Test", "Testing");
                return response;
            });

            var responseHeaders = await _client.SendAsync(HttpMethod.Head, "/resource");

            Assert.That(responseHeaders, Is.Not.Null);
            Assert.That(responseHeaders.GetValues("X-Test"), Contains.Item("Testing"));
        }

        [Test]
        public async Task SendAsync_Generic_ReturnsResponseObject()
        {
            var expectedResult = "This is the response content";

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new TestContent(expectedResult),
                };
            });

            var result = await _client.SendAsync<string>(HttpMethod.Get, "/resource");

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task DownlaodAsync_WhenResponseHasContent_ReturnsDownloadedStream()
        {
            var payload = "This is the request content";
            using var expectedStream = new MemoryStream([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                var content = new StreamContent(expectedStream);
                content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Octet);
                content.Headers.ContentLength = expectedStream.Length;

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = content,
                };
            });

            var resultStream = await _client.DownloadAsync(HttpMethod.Post, "/resource", new TestContent(payload), contentHeaders => new MemoryStream()) as MemoryStream;

            Assert.That(resultStream, Is.Not.Null);
            Assert.That(resultStream.ToArray(), Is.EqualTo(expectedStream.ToArray()));
        }

        [Test]
        public async Task DownlaodAsync_WhenResponseHasNoContent_ReturnsEmptyStream()
        {
            var payload = "This is the request content";

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var resultStream = await _client.DownloadAsync(HttpMethod.Post, "/resource", new TestContent(payload), contentHeaders => new MemoryStream());

            Assert.That(resultStream, Is.Not.Null);
            Assert.That(resultStream.Length, Is.Zero);
        }

        [Test]
        public void OnUnsupportedMediaType_ThrowsContentException()
        {
            using var responseContent = new StringContent("A,B", Encoding.UTF8, MediaTypeNames.Text.Csv);
            _mockMessageHandler.MockHttpResponse(HttpStatusCode.OK, responseContent);

            Assert.ThrowsAsync<HttpContentException>(async () => await _client.SendAsync<string[][]>(HttpMethod.Get, "/resource"));
        }

        [Test]
        public void OnUnsuccessfulStatusCode_WithoutResponseErrorType_ThrowsStandardRestException()
        {
            var errorDetails = new TestErrorResponse("You didn't provide the required data!");
            _mockMessageHandler.MockHttpResponse(HttpStatusCode.BadRequest, new TestContent(errorDetails));

            var exception = Assert.ThrowsAsync<HttpResponseException>(async () => await _client.SendAsync(HttpMethod.Get, "/resource"));

            Assert.That(exception, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(exception.ResponseMessage?.RequestMessage?.Method, Is.EqualTo(HttpMethod.Get));
                Assert.That(exception.ResponseMessage?.RequestMessage?.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                Assert.That(exception.Message, Is.Not.EqualTo(errorDetails.Message));
            });
        }

        [Test]
        public void OnUnsuccessfulStatusCode_WithResponseErrorType_ThrowsCustomizedRestException()
        {
            var errorDetails = new TestErrorResponse("You didn't provide the required data!");
            _mockMessageHandler.MockHttpResponse(HttpStatusCode.BadRequest, new TestContent(errorDetails));

            _client.ResponseErrorType = typeof(TestErrorResponse);
            var exception = Assert.ThrowsAsync<HttpResponseException>(async () => await _client.SendAsync(HttpMethod.Get, "/resource"));

            Assert.That(exception, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(exception.ResponseMessage?.RequestMessage?.Method, Is.EqualTo(HttpMethod.Get));
                Assert.That(exception.ResponseMessage?.RequestMessage?.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                Assert.That(exception.ResponseObject, Is.EqualTo(errorDetails).UsingPropertiesComparer());
                Assert.That(exception.Message, Is.EqualTo(errorDetails.Message));
            });
        }

        [Test]
        public async Task OnUnsuccessfulStatusCode_WithErrorHandler_RetriesRequest()
        {
            var expectedResult = "This is the response content";

            var handlerMock = new Mock<IHttpErrorHandler>();
            handlerMock.Setup(handler => handler.CanHandle(HttpStatusCode.MethodNotAllowed)).Returns(true);
            handlerMock.Setup(handler => handler.DecideOnRetryAsync(It.IsAny<HttpResponseErrorContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((HttpResponseErrorContext ctx, CancellationToken cancellationToken) =>
                {
                    if (!ctx.Request.IsCloned())
                    {
                        var newRequest = ctx.Request.Clone();
                        newRequest.Method = HttpMethod.Patch;
                        return HttpErrorHandlerResult.Retry(newRequest);
                    }
                    return HttpErrorHandlerResult.NoRetry;
                });
            var handler = handlerMock.Object;
            _client.ErrorHandlers.Add(handler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                Assert.That(request.Content, Is.Not.Null);
                Assert.That(request.Content.ReadAsStringAsync().Result, Is.EqualTo("test"));

                if (request.Method == HttpMethod.Patch)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new TestContent(expectedResult),
                    };
                }

                if (request.Method == HttpMethod.Put)
                {
                    return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var result = await _client.SendAsync<string>(HttpMethod.Put, "/resource", new StringContent("test"));

            Assert.Multiple(() =>
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(result, Is.EqualTo(expectedResult));
            });
        }

        [Test]
        public async Task OnConnectionFailure_UsesBackoffStrategy()
        {
            var maxRetries = 2;

            var mockBackoffStrategy = new Mock<IRetrySchedulerFactory>();
            var mockRetryScheduler = new Mock<IRetryScheduler>();

            var retries = 0;
            mockRetryScheduler.Setup(scheduler => scheduler.WaitAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => retries < maxRetries).Callback(() => ++retries);
            mockBackoffStrategy.Setup(strategy => strategy.CreateScheduler(It.IsAny<HttpRequestErrorContext>()))
                .Returns(mockRetryScheduler.Object);

            _client.BackoffStrategy = mockBackoffStrategy.Object;

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.That(request.Content, Is.Not.Null);
                Assert.That(request.Content.ReadAsStringAsync().Result, Is.EqualTo("test"));

                if (++attempts <= maxRetries)
                    throw new HttpRequestException("Connection failure", new SocketException((int)SocketError.HostUnreachable));

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _client.SendAsync(HttpMethod.Post, "/test", new StringContent("test"));

            mockRetryScheduler.Verify(scheduler => scheduler.WaitAsync(It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
            Assert.That(attempts, Is.EqualTo(maxRetries + 1));
        }
    }
}
