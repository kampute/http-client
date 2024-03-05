namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpRestClientExtensionsTests
    {
        private readonly TestContentDeserializer _testContentFormatter = new();
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
                BaseAddress = new Uri("http://api.test.com"),
            };
            _restClient.ResponseDeserializers.Add(_testContentFormatter);
        }

        [TearDown]
        public void Cleanup()
        {
            _restClient.Dispose();
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

            var actualResult = await _restClient.GetAsync<string>("/resource");

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

            var actualResult = await _restClient.PostAsync<string>("/resource", new TestContent(payload));

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

            var actualResult = await _restClient.PutAsync<string>("/resource", new TestContent(payload));

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

            var actualResult = await _restClient.PatchAsync<string>("/resource", new TestContent(payload));

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

            var actualResult = await _restClient.DeleteAsync<string>("/resource");

            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }
    }
}