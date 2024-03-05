namespace Kampute.HttpClient.Test.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.HttpClient.RetryStrategies;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
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
            var tooManyRequestsHandler = new HttpError429Handler();
            _client.ErrorHandlers.Add(tooManyRequestsHandler);

            var timer = new Stopwatch();
            var resetDelay = TimeSpan.FromSeconds(2); // The delay should be more than a second because the reset time is expressed as a Unix time in seconds.

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                if (++attempts == 1)
                    timer.Start();

                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.Add("x-rate-limit-reset", DateTimeOffset.UtcNow.Add(resetDelay).ToUnixTimeSeconds().ToString());
                return response;
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/rate-limited/resource"), Throws.TypeOf<HttpResponseException>());
            timer.Stop();

            Assert.Multiple(() =>
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(timer.Elapsed, Is.EqualTo(resetDelay).Within(TimeSpan.FromSeconds(1.0)));
            });
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
        public async Task On429Response_WithCustomBackoffStrategy_RetriesAccordingToPolicy()
        {
            var tooManyRequestsHandler = new HttpError429Handler
            {
                OnBackoffStrategy = (ctx, resetTime) => new UniformRetryStrategy(2, TimeSpan.FromMilliseconds(100))
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
