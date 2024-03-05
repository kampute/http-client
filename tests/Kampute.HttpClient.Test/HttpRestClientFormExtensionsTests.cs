namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpRestClientFormExtensionsTests
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
                BaseAddress = new Uri("http://api.test.com/form"),
            };
        }

        [TearDown]
        public void Cleanup()
        {
            _restClient.Dispose();
        }

        [Test]
        public async Task PostAsFormAsync_InvokesHttpClientCorrectly()
        {
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.FormUrlEncoded));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo("name=value"));
                });

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _restClient.PostAsFormAsync("/resource", [KeyValuePair.Create("name", "value")]);
        }

        [Test]
        public async Task PutAsFormAsync_InvokesHttpClientCorrectly()
        {
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.FormUrlEncoded));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo("name=value"));
                });

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _restClient.PutAsFormAsync("/resource", [KeyValuePair.Create("name", "value")]);
        }

        [Test]
        public async Task PatchAsFormAsync_InvokesHttpClientCorrectly()
        {
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Patch));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/resource")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.FormUrlEncoded));
                    Assert.That(request.Content?.ReadAsStringAsync().Result, Is.EqualTo("name=value"));
                });

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _restClient.PatchAsFormAsync("/resource", [KeyValuePair.Create("name", "value")]);
        }
    }
}