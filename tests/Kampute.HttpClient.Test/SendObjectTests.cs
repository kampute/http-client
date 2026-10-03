namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.Content;
    using Kampute.HttpClient.Content.Abstracts;
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class SendObjectTests
    {
        private const string ReadOnlyMediaType = "application/x-read-only";

        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private readonly List<(HttpRequestMessage Request, string? Body)> _sentRequests = [];
        private HttpRestClient _client;

        [SetUp]
        public void Setup()
        {
            _sentRequests.Clear();
            _mockMessageHandler.MockHttpResponse(request =>
            {
                _sentRequests.Add((request, request.Content?.ReadAsStringAsync().Result));
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new TestContent("done") };
            });

            var httpClient = new HttpClient(_mockMessageHandler.Object, disposeHandler: false);
            _client = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
        }

        [TearDown]
        public void Cleanup()
        {
            _client.Dispose();
        }

        [Test]
        public async Task SendObjectAsync_WithMediaType_WritesPayloadWithRegisteredFormatterIgnoringCase()
        {
            _client.ContentFormatters.Add(new TestContentFormatter());

            var result = await _client.SendObjectAsync<string>(HttpMethod.Post, "/resource", "payload", Constants.TestMediaType.ToUpperInvariant());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo("done"));
                Assert.That(_sentRequests, Has.Count.EqualTo(1));
                Assert.That(_sentRequests[0].Request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(Constants.TestMediaType));
                Assert.That(_sentRequests[0].Body, Is.EqualTo("payload"));
            }
        }

        [Test]
        public async Task SendObjectAsync_WithSeveralMatchingFormatters_UsesFirstRegistered()
        {
            _client.ContentFormatters.Add(new MarkingFormatter());
            _client.ContentFormatters.Add(new TestContentFormatter());

            await _client.SendObjectAsync(HttpMethod.Post, "/resource", "payload", Constants.TestMediaType);

            Assert.That(_sentRequests.Single().Body, Is.EqualTo("marked:payload"));
        }

        [Test]
        public async Task SendObjectAsync_WithFormatter_IgnoresRegisteredFormatters()
        {
            _client.ContentFormatters.Add(new TestContentFormatter());

            await _client.SendObjectAsync(HttpMethod.Post, "/resource", "payload", new MarkingFormatter());

            Assert.That(_sentRequests.Single().Body, Is.EqualTo("marked:payload"));
        }

        [Test]
        public void SendObjectAsync_WhenNoFormatterWritesMediaType_ThrowsBeforeSending()
        {
            _client.ContentFormatters.Add(new TestContentFormatter());

            var exception = Assert.Throws<InvalidOperationException>(() => _client.SendObjectAsync<string>(HttpMethod.Post, "/resource", "payload", "application/x-unknown"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.Message, Does.Contain("application/x-unknown"));
                Assert.That(_sentRequests, Is.Empty);
            }
        }

        [Test]
        public void SendObjectAsync_WhenFormatterCannotWritePayloadType_ThrowsBeforeSending()
        {
            Assert.Throws<InvalidOperationException>(() => _client.SendObjectAsync(HttpMethod.Post, "/resource", new object(), new MarkingFormatter()));
            Assert.That(_sentRequests, Is.Empty);
        }

        [Test]
        public async Task SendObjectAsync_WithHttpContentPayload_SendsContentAsItIs()
        {
            var firstContent = new StringContent("first", Encoding.UTF8, "text/plain");
            var secondContent = new StringContent("second", Encoding.UTF8, "text/plain");

            await _client.SendObjectAsync(HttpMethod.Post, "/resource", firstContent, "application/x-unknown");
            await _client.SendObjectAsync(HttpMethod.Post, "/resource", secondContent, new MarkingFormatter());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sentRequests.Select(sent => sent.Request.Content), Is.EqualTo(new HttpContent[] { firstContent, secondContent }));
                Assert.That(_sentRequests.Select(sent => sent.Body), Is.EqualTo(new[] { "first", "second" }));
            }
        }

        [Test]
        public async Task Request_WithSendOnlyFormatter_AddsNothingToAcceptHeader()
        {
            _client.ContentFormatters.Add(new FormUrlEncodedFormatter());
            _client.ContentFormatters.Add(new TestContentFormatter());

            await _client.GetAsync<string>("/resource");

            Assert.That(_sentRequests.Single().Request.Headers.Accept.Select(accept => accept.MediaType), Is.EqualTo(new[] { Constants.TestMediaType }));
        }

        [Test]
        public async Task Request_WithReceiveOnlyFormatter_AddsItsMediaTypesToAcceptHeader()
        {
            _client.ContentFormatters.Add(new ReadOnlyFormatter());
            _client.ContentFormatters.Add(new TestContentFormatter());

            await _client.GetAsync<string>("/resource");

            Assert.That(_sentRequests.Single().Request.Headers.Accept.Select(accept => accept.MediaType), Is.EqualTo(new[] { ReadOnlyMediaType, Constants.TestMediaType }));
        }

        private sealed class MarkingFormatter : HttpContentFormatter
        {
            public MarkingFormatter()
                : base([], [Constants.TestMediaType])
            {
            }

            protected override bool CanWriteType(Type payloadType) => payloadType == typeof(string);

            protected override HttpContent CreateContent(object payload, string mediaType)
            {
                return new StringContent($"marked:{payload}", Encoding.UTF8, mediaType);
            }
        }

        private sealed class ReadOnlyFormatter : HttpContentFormatter
        {
            public ReadOnlyFormatter()
                : base([ReadOnlyMediaType], [])
            {
            }

            protected override async Task<object?> ReadContentAsync(HttpContent content, Type modelType, System.Threading.CancellationToken cancellationToken)
            {
                return await content.ReadAsStringAsync(cancellationToken);
            }
        }
    }
}
