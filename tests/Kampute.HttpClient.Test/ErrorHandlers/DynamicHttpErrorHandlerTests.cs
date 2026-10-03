namespace Kampute.HttpClient.Test.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class DynamicHttpErrorHandlerTests
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
        public void OnErrorResponse_WithHandBuiltRetryRequest_KeepsRetryBudget()
        {
            const int maxAttempts = 10;
            var backoff = BackoffStrategies.Uniform(2, TimeSpan.Zero);

            _client.ErrorHandlers.Add(new DynamicHttpErrorHandler(async (ctx, ct) =>
            {
                var decision = await ctx.ScheduleRetryAsync(backoff, backoff.CreateScheduler, ct);
                if (decision.RequestToRetry is null)
                    return decision;

                decision.RequestToRetry.Dispose();
                return HttpErrorHandlerResult.Retry(new HttpRequestMessage(ctx.Request.Method, ctx.Request.RequestUri));
            }));

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request => Interlocked.Increment(ref attempts) < maxAttempts
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK));

            var exception = Assert.ThrowsAsync<HttpResponseException>(() => _client.SendAsync(HttpMethod.Get, "/resource"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(attempts, Is.EqualTo(3));
            }
        }
    }
}
