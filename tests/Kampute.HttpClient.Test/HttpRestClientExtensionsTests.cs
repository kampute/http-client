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
        public async Task HeadAsync_InvokesHttpClientCorrectly()
        {
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Head));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var responseHeaders = await _client.HeadAsync("/resource");

            Assert.That(responseHeaders, Is.Not.Null);
        }

        [Test]
        public async Task OptionsAsync_InvokesHttpClientCorrectly()
        {
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Options));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var responseHeaders = await _client.OptionsAsync("/resource");

            Assert.That(responseHeaders, Is.Not.Null);
        }

        [Test]
        public async Task GetAsync_ReturnsResponseAsObject()
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
        public async Task GetAsByteArrayAsync_ReturnsResponseAsBytes()
        {
            var expectedBytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

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
                    Content = new ByteArrayContent(expectedBytes),
                };
            });

            var resultBytes = await _client.GetAsByteArrayAsync("/resource");

            Assert.That(resultBytes, Is.EqualTo(expectedBytes));
        }

        [Test]
        public async Task GetAsStringAsync_ReturnsResponseAsString()
        {
            var expectedString = "This is a response message.";

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
                    Content = new StringContent(expectedString),
                };
            });

            var resultString = await _client.GetAsStringAsync("/resource");

            Assert.That(resultString, Is.EqualTo(expectedString));
        }

        [Test]
        public async Task GetAsStreamAsync_ReturnsResponseAsStream()
        {
            using var expectedStream = new MemoryStream([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

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
                    Content = new ByteArrayContent(expectedStream.ToArray()),
                };
            });

            using var resultStream = await _client.GetAsStreamAsync("/resource");

            Assert.That(resultStream, Is.EqualTo(expectedStream));
        }

        [Test]
        public async Task GetToStreamAsync_WritesResponseIntoStream()
        {
            using var expectedStream = new MemoryStream([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

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
                    Content = new ByteArrayContent(expectedStream.ToArray()),
                };
            });

            using var resultStream = new MemoryStream();
            await _client.GetToStreamAsync("/resource", resultStream);

            Assert.That(resultStream, Is.EqualTo(expectedStream));
        }

        [Test]
        public async Task PostAsync_InvokesHttpClientCorrectly()
        {
            var payload = "This is the request content";

            var sent = false;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo(payload));
                });

                sent = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _client.PostAsync("/resource", new TestContent(payload));

            Assert.That(sent, Is.True);
        }

        [Test]
        public async Task PostAsync_Generic_ReturnsResponseAsObject()
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

            var sent = false;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo(payload));
                });

                sent = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _client.PutAsync("/resource", new TestContent(payload));

            Assert.That(sent, Is.EqualTo(true));
        }

        [Test]
        public async Task PutAsync_Generic_ReturnsResponseAsObject()
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

            var sent = false;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Patch));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo(payload));
                });

                sent = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _client.PatchAsync("/resource", new TestContent(payload));

            Assert.That(sent, Is.EqualTo(true));
        }

        [Test]
        public async Task PatchAsync_Generic_ReturnsResponseAsObject()
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
        public async Task DeleteAsync_Generic_ReturnsResponseAsObject()
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
        public async Task DownloadAsync_WritesResponseIntoStream()
        {
            var method = new HttpMethod("TEST");
            var payload = "This is the request content";
            using var expectedStream = new MemoryStream([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(method));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                });

                var content = new ByteArrayContent(expectedStream.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Octet);

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = content,
                };
            });

            using var resultStream = await _client.DownloadAsync(method, "/resource", new TestContent(payload), contentHeaders =>
            {
                Assert.That(contentHeaders, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(contentHeaders.ContentType, Is.EqualTo(new MediaTypeHeaderValue(MediaTypeNames.Application.Octet)));
                    Assert.That(contentHeaders.ContentLength, Is.EqualTo(expectedStream.Length));
                });
                return new MemoryStream((int)contentHeaders.ContentLength.GetValueOrDefault());
            });

            Assert.That(resultStream, Is.Not.Null);
            Assert.Multiple(() =>
            {
                resultStream.Seek(0, SeekOrigin.Begin);
                Assert.That(resultStream, Is.TypeOf<MemoryStream>());
                Assert.That(resultStream, Is.EqualTo(expectedStream));
            });
        }
    }
}
