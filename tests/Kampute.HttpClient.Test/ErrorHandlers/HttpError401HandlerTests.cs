namespace Kampute.HttpClient.Test.ErrorHandlers
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpError401HandlerTests
    {
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _client;

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, false);
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
        public async Task On401Response_BySuccessfulAuthentication_AuthorizesRequests()
        {
            var scheme = AuthSchemes.ApiKey;
            var apiKey = "Key";

            var numberOfInvokes = 0;
            var numberOfResponses = 0;
            var numberOfRequests = 100;

            using var unauthorizeHandler = new HttpError401Handler((_, _) =>
            {
                Interlocked.Increment(ref numberOfInvokes);
                var authorization = new AuthenticationHeaderValue(scheme, apiKey);
                return Task.FromResult<AuthenticationHeaderValue?>(authorization);
            });

            _client.ErrorHandlers.Add(unauthorizeHandler);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Interlocked.Increment(ref numberOfResponses);

                if (_client.DefaultRequestHeaders.Authorization is not null)
                    return new HttpResponseMessage(HttpStatusCode.OK);

                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            });

            var tasks = Enumerable.Range(1, numberOfRequests).Select(i => _client.SendAsync(HttpMethod.Get, $"/protected/resource{i}"));
            await Task.WhenAll(tasks);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(numberOfInvokes, Is.EqualTo(1));
                Assert.That(numberOfResponses, Is.EqualTo(numberOfRequests + 1));
                Assert.That(_client.DefaultRequestHeaders.Authorization, Is.Not.Null);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Scheme, Is.EqualTo(scheme));
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Parameter, Is.EqualTo(apiKey));
                }
            }
        }

        [Test]
        public async Task On401Response_ByFailedAuthentication_ThrowsUnauthorizedHttpError()
        {
            using var unauthorizeHandler = new HttpError401Handler((ctx, ct) => ctx.Client.SendAsync<AuthenticationHeaderValue?>(HttpMethod.Get, "/authenticate", null, ct));

            _client.ErrorHandlers.Add(unauthorizeHandler);

            _mockMessageHandler.MockHttpResponse(request => new HttpResponseMessage(HttpStatusCode.Unauthorized));

            var caughtException = default(HttpResponseException);
            try
            {
                await _client.SendAsync(HttpMethod.Get, "/protected/resource");
            }
            catch (HttpResponseException ex)
            {
                caughtException = ex;
            }

            Assert.That(caughtException, Is.Not.Null);
            Assert.That(caughtException.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
