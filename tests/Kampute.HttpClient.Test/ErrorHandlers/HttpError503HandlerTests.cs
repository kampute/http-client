namespace Kampute.HttpClient.Test.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpError503HandlerTests
    {
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _client;

        [SetUp]
        public void Setup()
        {
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
        public async Task On503Response_WithRetryAfterHeader_AsDate_RetriesRequestAfterSpecifiedTime()
        {
            var retryDelay = TimeSpan.FromMilliseconds(1000);
            var retryTime = DateTimeOffset.UtcNow.Add(retryDelay);
            var actualRetryTime = default(DateTimeOffset?);
            var serviceUnavailableHandler = new HttpError503Handler
            {
                OnBackoffStrategy = (ctx, retryAfter) =>
                {
                    actualRetryTime = retryAfter;
                    return BackoffStrategies.Uniform(1, TimeSpan.Zero);
                }
            };
            _client.ErrorHandlers.Add(serviceUnavailableHandler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(retryTime);
                return response;
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(actualRetryTime, Is.EqualTo(retryTime).Within(TimeSpan.FromSeconds(1)));
            }
        }

        [Test]
        public async Task On503Response_WithRetryAfterHeader_AsDelta_RetriesRequestAfterSpecifiedDelay()
        {
            var retryDelay = TimeSpan.FromMilliseconds(1000);
            var actualRetryTime = default(DateTimeOffset?);
            var serviceUnavailableHandler = new HttpError503Handler
            {
                OnBackoffStrategy = (ctx, retryAfter) =>
                {
                    actualRetryTime = retryAfter;
                    return BackoffStrategies.Uniform(1, TimeSpan.Zero);
                }
            };
            _client.ErrorHandlers.Add(serviceUnavailableHandler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(retryDelay);
                return response;
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(actualRetryTime, Is.EqualTo(DateTimeOffset.UtcNow.Add(retryDelay)).Within(TimeSpan.FromSeconds(1)));
            }
        }

        [Test]
        public async Task On503Response_WithoutRetryAfterHeader_RetriesAccordingToDefaultStrategy()
        {
            var serviceUnavailableHandler = new HttpError503Handler();
            _client.ErrorHandlers.Add(serviceUnavailableHandler);
            _client.BackoffStrategy = BackoffStrategies.Uniform(2, TimeSpan.Zero);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());

            Assert.That(attempts, Is.EqualTo(3));
        }

        [Test]
        public async Task On503Response_WithCustomBackoffStrategy_RetriesAccordingToCustomStrategy()
        {
            var serviceUnavailableHandler = new HttpError503Handler
            {
                OnBackoffStrategy = (ctx, retryAfter) => BackoffStrategies.Uniform(2, TimeSpan.Zero)
            };
            _client.ErrorHandlers.Add(serviceUnavailableHandler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());

            Assert.That(attempts, Is.EqualTo(3));
        }
    }
}
