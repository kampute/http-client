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
            var serviceUnavailableHandler = new HttpError503Handler();
            _client.ErrorHandlers.Add(serviceUnavailableHandler);


            var timer = new Stopwatch();
            var retryDelay = TimeSpan.FromMilliseconds(100);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                if (++attempts == 1)
                    timer.Start();

                var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.Add(retryDelay));
                return response;
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());
            timer.Stop();

            Assert.Multiple(() =>
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(timer.Elapsed, Is.InRange(retryDelay, 1.5 * retryDelay));
            });
        }

        [Test]
        public async Task On503Response_WithRetryAfterHeader_AsDelta_RetriesRequestAfterSpecifiedDelay()
        {
            var serviceUnavailableHandler = new HttpError503Handler();
            _client.ErrorHandlers.Add(serviceUnavailableHandler);

            var timer = new Stopwatch();
            var retryDelay = TimeSpan.FromMilliseconds(100);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                if (++attempts == 1)
                    timer.Start();

                var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(retryDelay);
                return response;
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());
            timer.Stop();

            Assert.Multiple(() =>
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(timer.Elapsed, Is.InRange(retryDelay, 1.5 * retryDelay));
            });
        }

        [Test]
        public async Task On503Response_WithoutRetryAfterHeader_RetriesAccordingToDefaultPolicy()
        {
            var serviceUnavailableHandler = new HttpError503Handler();
            _client.ErrorHandlers.Add(serviceUnavailableHandler);
            _client.BackoffStrategy = new UniformRetryStrategy(2, TimeSpan.FromMilliseconds(100));

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
        public async Task On503Response_WithoutRetryAfterHeader_RetriesAccordingToClientPolicy()
        {
            var serviceUnavailableHandler = new HttpError503Handler();
            _client.ErrorHandlers.Add(serviceUnavailableHandler);
            _client.BackoffStrategy = new UniformRetryStrategy(2, TimeSpan.FromMilliseconds(100));

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
        public async Task On503Response_WithCustomBackoffStrategy_RetriesAccordingToPolicy()
        {
            var serviceUnavailableHandler = new HttpError503Handler
            {
                OnBackoffStrategy = (ctx, retryAfter) => new UniformRetryStrategy(2, TimeSpan.FromMilliseconds(100))
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
