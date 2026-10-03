namespace Kampute.HttpClient.Test.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.HttpClient.TestSupport;
    using Kampute.Retry;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
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
        public void OnErrorResponse_InvokesDelegateWithResponseContext()
        {
            var seenStatusCodes = new List<HttpStatusCode>();
            _client.ErrorHandlers.Add(new DynamicHttpErrorHandler((ctx, _) =>
            {
                seenStatusCodes.Add(ctx.Response.StatusCode);
                return Task.FromResult(HttpErrorHandlerResult.NoRetry);
            }));

            _mockMessageHandler.MockHttpResponse(HttpStatusCode.Conflict);

            var exception = Assert.ThrowsAsync<HttpResponseException>(() => _client.SendAsync(HttpMethod.Get, "/resource"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(seenStatusCodes, Is.EqualTo(new[] { HttpStatusCode.Conflict }));
            }
        }

        [Test]
        public void OnErrorResponse_WithHandBuiltRetryRequest_KeepsRetryBudget()
        {
            const int maxAttempts = 10;
            var backoff = RetryStrategies.Uniform(TimeSpan.Zero).WithMaxAttempts(2).ToHttpRetryPolicy();

            _client.ErrorHandlers.Add(new DynamicHttpErrorHandler(async (ctx, ct) =>
            {
                var decision = await ctx.ScheduleRetryAsync(backoff, backoff.CreateSession, ct);
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

        [Test]
        public async Task OnErrorResponse_WithHandBuiltRetryRequestReusingOriginalContent_SendsOriginalBodyOnLaterRetries()
        {
            _client.RetryPolicy = RetryStrategies.Uniform(TimeSpan.Zero).WithMaxAttempts(1).ToHttpRetryPolicy();
            _client.ErrorHandlers.Add(new DynamicHttpErrorHandler((ctx, _) =>
            {
                var retryRequest = new HttpRequestMessage(ctx.Request.Method, ctx.Request.RequestUri) { Content = ctx.Request.Content };
                return Task.FromResult(HttpErrorHandlerResult.Retry(retryRequest));
            }));

            var sentBodies = MockServiceUnavailableThenConnectionFailureThenSuccess();

            using var response = await _client.SendAsync(HttpMethod.Post, "/resource", new StringContent("original"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(sentBodies, Is.EqualTo(new[] { "original", "original", "original" }));
            }
        }

        [Test]
        public async Task OnErrorResponse_WithHandBuiltRetryRequestWithNewContent_SendsNewBodyOnLaterRetries()
        {
            _client.RetryPolicy = RetryStrategies.Uniform(TimeSpan.Zero).WithMaxAttempts(1).ToHttpRetryPolicy();
            _client.ErrorHandlers.Add(new DynamicHttpErrorHandler((ctx, _) =>
            {
                var retryRequest = new HttpRequestMessage(ctx.Request.Method, ctx.Request.RequestUri) { Content = new StringContent("replacement") };
                return Task.FromResult(HttpErrorHandlerResult.Retry(retryRequest));
            }));

            var sentBodies = MockServiceUnavailableThenConnectionFailureThenSuccess();

            using var response = await _client.SendAsync(HttpMethod.Post, "/resource", new StringContent("original"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(sentBodies, Is.EqualTo(new[] { "original", "replacement", "replacement" }));
            }
        }

        private List<string> MockServiceUnavailableThenConnectionFailureThenSuccess()
        {
            var sentBodies = new List<string>();
            _mockMessageHandler.MockHttpResponse(request =>
            {
                sentBodies.Add(request.Content!.ReadAsStringAsync().Result);
                return sentBodies.Count switch
                {
                    1 => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                    2 => throw new HttpRequestException("Connection failure", new SocketException((int)SocketError.HostUnreachable)),
                    _ => new HttpResponseMessage(HttpStatusCode.OK),
                };
            });
            return sentBodies;
        }
    }
}
