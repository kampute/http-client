namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpRestClientExtensionsTests
    {
        private readonly TestContentDeserializer _testContentFormatter = new();
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _client;

        private Uri AbsoluteUrl(string url)
        {
            return _client.BaseAddress is not null
                ? new Uri(_client.BaseAddress, url)
                : new Uri(url);
        }

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
        public async Task GetAsync_InvokesHttpClientCorrectly()
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

            var actualResult = await _client.GetAsync<string>("/resource");

            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task PostAsync_InvokesHttpClientCorrectly()
        {
            var payload = "This is the request content";
            var expectedResult = "This is the response content";

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo(payload));
                });

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new TestContent(expectedResult),
                };
            });

            var actualResult = await _client.PostAsync<string>("/resource", new TestContent(payload));

            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task PutAsync_InvokesHttpClientCorrectly()
        {
            var payload = "This is the request content";
            var expectedResult = "This is the response content";

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo(payload));
                });

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new TestContent(expectedResult),
                };
            });

            var actualResult = await _client.PutAsync<string>("/resource", new TestContent(payload));

            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task PatchAsync_InvokesHttpClientCorrectly()
        {
            var payload = "This is the request content";
            var expectedResult = "This is the response content";

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Patch));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo(payload));
                });

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new TestContent(expectedResult),
                };
            });

            var actualResult = await _client.PatchAsync<string>("/resource", new TestContent(payload));

            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task DeleteAsync_InvokesHttpClientCorrectly()
        {
            var expectedResult = "This is the response content";

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Delete));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new TestContent(expectedResult),
                };
            });

            var actualResult = await _client.DeleteAsync<string>("/resource");

            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task FetchToStreamAsync_LoadsStreamAndReturnsContentHeaders()
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

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = content,
                };
            });

            using var resultStream = new MemoryStream();
            var contentHeaders = await _client.FetchToStreamAsync(resultStream, HttpMethod.Post, "/resource", new TestContent(payload));

            Assert.That(contentHeaders, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(contentHeaders.ContentType, Is.EqualTo(new MediaTypeHeaderValue(MediaTypeNames.Application.Octet)));
                Assert.That(contentHeaders.ContentLength, Is.EqualTo(resultStream.Length));
                Assert.That(resultStream.ToArray(), Is.EqualTo(expectedStream.ToArray()));
            });
        }
    }
}
