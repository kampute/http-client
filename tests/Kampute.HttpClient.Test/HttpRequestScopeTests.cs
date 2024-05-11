namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpRequestScopeTests
    {
        private HttpRestClient _client;
        private HttpRequestScope _scope;
        private Mock<HttpMessageHandler> _mockMessageHandler;

        [SetUp]
        public void Setup()
        {
            _mockMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockMessageHandler.Object, false);
            _client = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
            _scope = new HttpRequestScope(_client);
        }

        [TearDown]
        public void Cleanup()
        {
            _client.Dispose();
        }

        [Test]
        public void SetProperty_AddsPropertyToScope()
        {
            var scopedProperty = new KeyValuePair<string, object>("PROP_NAME", "PROP_VALUE");

            _scope.SetProperty(scopedProperty.Key, scopedProperty.Value);

            Assert.That(_scope.Properties, Has.Count.EqualTo(1));
            Assert.That(_scope.Properties, Contains.Item(scopedProperty));
        }

        [Test]
        public void UnsetProperty_RemovesPropertyFromScope()
        {
            var scopedProperty = new KeyValuePair<string, object?>("PROP_NAME", null);

            _scope.UnsetProperty(scopedProperty.Key);

            Assert.That(_scope.Properties, Has.Count.EqualTo(1));
            Assert.That(_scope.Properties, Contains.Item(scopedProperty));
        }

        [Test]
        public void SetHeader_AddsHeaderToScope()
        {
            var scopedHeader = new KeyValuePair<string, string>("HEADER_NAME", "HEADER_VALUE");

            _scope.SetHeader(scopedHeader.Key, scopedHeader.Value);

            Assert.That(_scope.Headers, Has.Count.EqualTo(1));
            Assert.That(_scope.Headers, Contains.Item(scopedHeader));
        }

        [Test]
        public void UnsetHeader_RemovesHeaderFromScope()
        {
            var scopedHeader = new KeyValuePair<string, string?>("HEADER_NAME", null);

            _scope.UnsetHeader(scopedHeader.Key);

            Assert.That(_scope.Headers, Has.Count.EqualTo(1));
            Assert.That(_scope.Headers, Contains.Item(scopedHeader));
        }

        [Test]
        public async Task PerformAsync_AppliesScopedHeadersAndProperties()
        {
            var propertyName = "PROP_NAME";
            var propertyValue = "PROP_VALUE";
            var headerName = "HEADER_NAME";
            var headerValue = "HEADER_VALUE";

            _scope.SetProperty(propertyName, propertyValue).SetHeader(headerName, headerValue);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                var propExists = request.Options.TryGetValue(new HttpRequestOptionsKey<string>(propertyName), out var propValue);
                Assert.Multiple(() =>
                {
                    Assert.That(propExists, Is.True);
                    Assert.That(propValue, Is.EqualTo(propertyValue));
                    Assert.That(request.Headers.GetValues(headerName), Is.EqualTo(new[] { headerValue }));
                });

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _scope.PerformAsync(async client =>
            {
                await client.SendAsync(HttpMethod.Get, "/test");
            });
        }
    }
}
