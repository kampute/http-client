namespace Kampute.HttpClient.NewtonsoftJson.Test
{
    using Kampute.HttpClient;
    using Moq;
    using Moq.Protected;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

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

        private void MockHttpResponse(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
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
                    (HttpRequestMessage request, CancellationToken _) => responseFactory(request)
                );
        }

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, false);
            _restClient = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com/json"),
            };
            _restClient.AcceptJson(TestModel.JsonSettings);
            _restClient.SetJsonSerializerSettings(TestModel.JsonSettings);
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

            MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                });

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

            MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                });

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

            MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Patch));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                });

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
    }
}
