namespace Kampute.HttpClient.Test.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.HttpClient.ErrorHandlers.Abstracts;
    using Kampute.HttpClient.TestSupport;
    using Kampute.Retry;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class RetryableHttpErrorHandlerTests
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
        public void MaxRetryDelay_DefaultsToFiveMinutes()
        {
            var handler = new HttpError503Handler();

            Assert.That(handler.MaxRetryDelay, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void MaxRetryDelay_WhenNegative_ThrowsArgumentOutOfRangeException()
        {
            var handler = new HttpError503Handler();

            Assert.Throws<ArgumentOutOfRangeException>(() => handler.MaxRetryDelay = TimeSpan.FromSeconds(-1));
        }

        [Test]
        public void OnSuggestedDelayAboveMaxRetryDelay_DoesNotRetry()
        {
            var strategyRequested = false;
            var handler = new HttpError503Handler
            {
                MaxRetryDelay = TimeSpan.FromMinutes(1),
                OnRetryPolicy = (_, _) =>
                {
                    strategyRequested = true;
                    return RetryTestHelpers.MockRetryPolicy(1, out _).Object;
                }
            };
            _client.ErrorHandlers.Add(handler);

            var attempts = MockServiceUnavailable(TimeSpan.FromHours(1));

            var exception = Assert.ThrowsAsync<HttpResponseException>(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(attempts(), Is.EqualTo(1));
                Assert.That(strategyRequested, Is.False);
            }
        }

        [Test]
        public async Task OnSuggestedDelayBelowMaxRetryDelay_Retries()
        {
            var handler = new HttpError503Handler
            {
                MaxRetryDelay = TimeSpan.FromMinutes(1),
                OnRetryPolicy = (_, _) => RetryTestHelpers.MockRetryPolicy(1, out _).Object
            };
            _client.ErrorHandlers.Add(handler);

            var attempts = MockServiceUnavailable(TimeSpan.FromSeconds(30));

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());

            Assert.That(attempts(), Is.EqualTo(2));
        }

        [Test]
        public async Task OnLongSuggestedDelay_WithoutMaxRetryDelay_Retries()
        {
            var handler = new HttpError503Handler
            {
                MaxRetryDelay = null,
                OnRetryPolicy = (_, _) => RetryTestHelpers.MockRetryPolicy(1, out _).Object
            };
            _client.ErrorHandlers.Add(handler);

            var attempts = MockServiceUnavailable(TimeSpan.FromHours(1));

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());

            Assert.That(attempts(), Is.EqualTo(2));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task OnLongSuggestedDelay_AfterRetrySessionCreated_RespectsMaxRetryDelay(bool useRateLimitReset, bool unlimitedDelay)
        {
            RetryableHttpErrorHandler handler = useRateLimitReset ? new HttpError429Handler() : new HttpError503Handler();
            handler.MaxRetryDelay = unlimitedDelay ? null : TimeSpan.FromMinutes(1);
            var policyRequests = 0;
            handler.OnRetryPolicy = (_, _) =>
            {
                ++policyRequests;
                return RetryStrategies.Uniform(TimeSpan.Zero).WithMaxAttempts(2).ToHttpRetryPolicy();
            };
            _client.ErrorHandlers.Add(handler);

            var statusCode = useRateLimitReset ? HttpStatusCode.TooManyRequests : HttpStatusCode.ServiceUnavailable;
            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(_ =>
            {
                if (++attempts > 2)
                    return new HttpResponseMessage(HttpStatusCode.OK);

                var response = new HttpResponseMessage(statusCode);
                if (attempts == 2)
                {
                    if (useRateLimitReset)
                        response.Headers.Add("x-rate-limit-reset", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString());
                    else
                        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(1));
                }
                return response;
            });

            if (unlimitedDelay)
            {
                using var response = await _client.SendAsync(HttpMethod.Get, "/resource");
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
            else
            {
                var exception = Assert.ThrowsAsync<HttpResponseException>(() => _client.SendAsync(HttpMethod.Get, "/resource"));
                Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attempts, Is.EqualTo(unlimitedDelay ? 3 : 2));
                Assert.That(policyRequests, Is.EqualTo(1));
            }
        }

        [Test]
        public void OnRateLimitResetAboveMaxRetryDelay_DoesNotRetry()
        {
            var handler = new HttpError429Handler
            {
                OnRetryPolicy = (_, _) => RetryTestHelpers.MockRetryPolicy(1, out _).Object
            };
            _client.ErrorHandlers.Add(handler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Interlocked.Increment(ref attempts);

                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.Add("x-rate-limit-reset", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString());
                return response;
            });

            Assert.ThrowsAsync<HttpResponseException>(() => _client.SendAsync(HttpMethod.Get, "/rate-limited/resource"));

            Assert.That(attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task OnConnectionFailureThenRateLimit_RetriesAtSuggestedTime()
        {
            var suggestedDelay = TimeSpan.FromSeconds(1);
            _client.RetryPolicy = RetryStrategies.Uniform(TimeSpan.Zero).WithMaxAttempts(1).ToHttpRetryPolicy();
            _client.ErrorHandlers.Add(new HttpError429Handler());

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                switch (Interlocked.Increment(ref attempts))
                {
                    case 1:
                        throw new HttpRequestException("Connection failure", new SocketException((int)SocketError.HostUnreachable));
                    case 2:
                        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                        response.Headers.RetryAfter = new RetryConditionHeaderValue(suggestedDelay);
                        return response;
                    default:
                        return new HttpResponseMessage(HttpStatusCode.OK);
                }
            });

            var timer = Stopwatch.StartNew();
            using var response = await _client.SendAsync(HttpMethod.Get, "/resource");
            timer.Stop();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(attempts, Is.EqualTo(3));
                Assert.That(timer.Elapsed, Is.GreaterThanOrEqualTo(suggestedDelay - TimeSpan.FromMilliseconds(100)));
            }
        }

        [Test]
        public async Task OnServiceUnavailableThenConnectionFailure_RetriesWithRetryPolicy()
        {
            var mockRetryPolicy = RetryTestHelpers.MockRetryPolicy(1, out var mockRetrySession);
            _client.RetryPolicy = mockRetryPolicy.Object;
            _client.ErrorHandlers.Add(new HttpError503Handler());

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                switch (Interlocked.Increment(ref attempts))
                {
                    case 1:
                        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
                        return response;
                    case 2:
                        throw new HttpRequestException("Connection failure", new SocketException((int)SocketError.HostUnreachable));
                    default:
                        return new HttpResponseMessage(HttpStatusCode.OK);
                }
            });

            using var response = await _client.SendAsync(HttpMethod.Get, "/resource");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(attempts, Is.EqualTo(3));
            }
            mockRetrySession.Verify(scheduler => scheduler.WaitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private Func<int> MockServiceUnavailable(TimeSpan retryAfter)
        {
            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                Interlocked.Increment(ref attempts);

                var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter);
                return response;
            });
            return () => attempts;
        }
    }
}
