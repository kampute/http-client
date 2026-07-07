namespace Kampute.HttpClient.Test.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpError429HandlerTests
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
        public async Task On429Response_WithRateLimitResetHeader_RetriesRequestAfterSpecifiedTime()
        {
            var resetDelay = TimeSpan.FromSeconds(2); // The delay should be more than a second because the reset time is expressed as a Unix time in seconds.
            var resetTime = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.Add(resetDelay).ToUnixTimeSeconds());
            var actualResetTime = default(DateTimeOffset?);
            var tooManyRequestsHandler = new HttpError429Handler
            {
                OnBackoffStrategy = (ctx, retryAfter) =>
                {
                    actualResetTime = retryAfter;
                    return BackoffStrategies.Uniform(1, TimeSpan.Zero);
                }
            };
            _client.ErrorHandlers.Add(tooManyRequestsHandler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.Add("x-rate-limit-reset", resetTime.ToUnixTimeSeconds().ToString());
                return response;
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/rate-limited/resource"), Throws.TypeOf<HttpResponseException>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(actualResetTime, Is.EqualTo(resetTime));
            }
        }

        [Test]
        public async Task On429Response_WithoutRateLimitResetHeader_DoesNotRetry()
        {
            var tooManyRequestsHandler = new HttpError429Handler();
            _client.ErrorHandlers.Add(tooManyRequestsHandler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/rate-limited/resource"), Throws.TypeOf<HttpResponseException>());

            Assert.That(attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task On429Response_WithCustomBackoffStrategy_RetriesAccordingToCustomStrategy()
        {
            var tooManyRequestsHandler = new HttpError429Handler
            {
                OnBackoffStrategy = (ctx, resetTime) => BackoffStrategies.Uniform(2, TimeSpan.Zero)
            };
            _client.ErrorHandlers.Add(tooManyRequestsHandler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/rate-limited/resource"), Throws.TypeOf<HttpResponseException>());

            Assert.That(attempts, Is.EqualTo(3));
        }
    }
}
