namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
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
        private static readonly HttpMethod TestHttpMethod = new("TEST");

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
        public async Task SendAsync_CopiesDefaultHeadersToRequestHeaders()
        {
            var testerAgent = new ProductInfoHeaderValue("Tester", "1.0");
            _client.DefaultRequestHeaders.UserAgent.Add(testerAgent);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.That(request.Headers.UserAgent, Contains.Item(testerAgent));
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            await _client.SendAsync(TestHttpMethod, "/resource");
        }

        [Test]
        public async Task SendAsync_NonGeneric_ReturnsResponse()
        {
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(TestHttpMethod));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Headers.Add("X-Test", "Testing");
                return response;
            });

            var response = await _client.SendAsync(TestHttpMethod, "/resource");

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Headers.GetValues("X-Test"), Contains.Item("Testing"));
        }

        [Test]
        public async Task SendAsync_Generic_ReturnsResponseAsObject()
        {
            var expectedResult = "This is the response content";

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(TestHttpMethod));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new TestContent(expectedResult),
                };
            });

            var result = await _client.SendAsync<string>(TestHttpMethod, "/resource");

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void OnUnsupportedMediaType_ThrowsContentException()
        {
            using var responseContent = new StringContent("A,B", Encoding.UTF8, MediaTypeNames.Text.Csv);
            _mockMessageHandler.MockHttpResponse(HttpStatusCode.OK, responseContent);

            Assert.ThrowsAsync<HttpContentException>(async () => await _client.SendAsync<string[][]>(TestHttpMethod, "/resource"));
        }

        [Test]
        public void OnUnsuccessfulStatusCode_WithoutResponseErrorType_ThrowsStandardRestException()
        {
            var errorDetails = new TestErrorResponse("You didn't provide the required data!");
            _mockMessageHandler.MockHttpResponse(HttpStatusCode.BadRequest, new TestContent(errorDetails));

            var exception = Assert.ThrowsAsync<HttpResponseException>(async () => await _client.SendAsync(TestHttpMethod, "/resource"));

            Assert.That(exception, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(exception.ResponseMessage?.RequestMessage?.Method, Is.EqualTo(TestHttpMethod));
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
            var exception = Assert.ThrowsAsync<HttpResponseException>(async () => await _client.SendAsync(TestHttpMethod, "/resource"));

            Assert.That(exception, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(exception.ResponseMessage?.RequestMessage?.Method, Is.EqualTo(TestHttpMethod));
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
                        newRequest.Method = TestHttpMethod;
                        return HttpErrorHandlerResult.Retry(newRequest);
                    }
                    return HttpErrorHandlerResult.NoRetry;
                });
            var handler = handlerMock.Object;
            _client.ErrorHandlers.Add(handler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                ++attempts;

                Assert.That(request.Content, Is.Not.Null);
                Assert.That(request.Content.ReadAsStringAsync().Result, Is.EqualTo("test"));

                if (request.Method != TestHttpMethod)
                    return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new TestContent(expectedResult),
                };
            });

            var result = await _client.SendAsync<string>(HttpMethod.Get, "/resource", new StringContent("test"));

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

            var mockBackoffStrategy = new Mock<IHttpBackoffProvider>();
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

            await _client.SendAsync(TestHttpMethod, "/test", new StringContent("test"));

            mockRetryScheduler.Verify(scheduler => scheduler.WaitAsync(It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
            Assert.That(attempts, Is.EqualTo(maxRetries + 1));
        }

        [Test]
        public async Task BeginPropertyScope_AddsPropertiesToRequest()
        {
            var customPropName = "PROP_NAME";
            var customPropValue = "PROP_VALUE";

            var sent = false;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                var propExists = request.Options.TryGetValue(new HttpRequestOptionsKey<string>(customPropName), out var propValue);
                Assert.Multiple(() =>
                {
                    Assert.That(propExists, Is.True);
                    Assert.That(propValue, Is.EqualTo(customPropValue));
                });

                sent = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using var scope = _client.BeginPropertyScope(new Dictionary<string, object>
            {
                [customPropName] = customPropValue
            });

            using var _ = await _client.SendAsync(TestHttpMethod, "/resource");

            Assert.That(sent, Is.True);
        }

        [Test]
        public async Task BeginHeaderScope_AddsHeadersToRequest()
        {
            var customHeaderName = "HEADER_NAME";
            var customHeaderValue = "HEADER_VALUE";

            var sent = false;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.That(request.Headers.GetValues(customHeaderName), Is.EquivalentTo(new[] { customHeaderValue }));

                sent = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using var scope = _client.BeginHeaderScope(new Dictionary<string, string>
            {
                [customHeaderName] = customHeaderValue
            });

            using var _ = await _client.SendAsync(TestHttpMethod, "/resource");

            Assert.That(sent, Is.True);
        }
    }
}
