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
        public async Task TryAuthenticateAsync_OnSuccess_UpdatesAuthorizationHeader()
        {
            var authScheme = AuthSchemes.Bearer;
            var authParameter = "Testing";
            var authToken = "Token";

            using var unauthorizeHandler = new HttpError401Handler((_, challenges, _) =>
            {
                var authorization = challenges
                    .Where(x => x.Scheme == authScheme)
                    .Select(x => new AuthenticationHeaderValue(x.Scheme, authToken))
                    .FirstOrDefault();
                return Task.FromResult(authorization);
            });

            _client.ErrorHandlers.Add(unauthorizeHandler);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue($"Old-{authScheme}", $"Old-{authToken}");

            var authenticated = await unauthorizeHandler.TryAuthenticateAsync(_client, authScheme, authParameter);

            Assert.Multiple(() =>
            {
                Assert.That(authenticated, Is.True);
                Assert.That(_client.DefaultRequestHeaders.Authorization, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Scheme, Is.EqualTo(authScheme));
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Parameter, Is.EqualTo(authToken));
                });
            });
        }

        [Test]
        public async Task TryAuthenticateAsync_OnFailure_DoesNotUpdateAuthorizationHeader()
        {
            var authScheme = AuthSchemes.Bearer;
            var authToken = "Token";

            using var unauthorizeHandler = new HttpError401Handler((_, _, _) => Task.FromResult<AuthenticationHeaderValue?>(null));

            _client.ErrorHandlers.Add(unauthorizeHandler);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authScheme, authToken);

            var authenticated = await unauthorizeHandler.TryAuthenticateAsync(_client, authScheme);

            Assert.Multiple(() =>
            {
                Assert.That(authenticated, Is.False);
                Assert.That(_client.DefaultRequestHeaders.Authorization, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Scheme, Is.EqualTo(authScheme));
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Parameter, Is.EqualTo(authToken));
                });
            });
        }

        [Test]
        public async Task TryAuthenticateAsync_ForConcurrentCalls_OnlyOnceInvokesOnAuthenticationChallenge()
        {
            var authScheme = AuthSchemes.Bearer;
            var authToken = "Token";

            var numberOfCalls = 32;
            var numberOfInvokes = 0;
            var allAuthenticationsInitiated = new TaskCompletionSource<bool>();

            using var unauthorizeHandler = new HttpError401Handler(async (_, challenges, _) =>
            {
                Interlocked.Increment(ref numberOfInvokes);

                await allAuthenticationsInitiated.Task;

                return challenges
                    .Where(x => x.Scheme == authScheme)
                    .Select(x => new AuthenticationHeaderValue(x.Scheme, authToken))
                    .FirstOrDefault();
            });

            _client.ErrorHandlers.Add(unauthorizeHandler);

            var calls = Enumerable
                .Range(1, numberOfCalls)
                .Select(_ => Task.Run(async () => await unauthorizeHandler.TryAuthenticateAsync(_client, authScheme)))
                .ToArray();

            await Task.Run(() => allAuthenticationsInitiated.TrySetResult(true));

            var results = await Task.WhenAll(calls);

            Assert.Multiple(() =>
            {
                Assert.That(numberOfInvokes, Is.EqualTo(1));
                Assert.That(results, Is.All.True);
                Assert.That(_client.DefaultRequestHeaders.Authorization, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Scheme, Is.EqualTo(authScheme));
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Parameter, Is.EqualTo(authToken));
                });
            });
        }

        [Test]
        public async Task On401Response_InvokesOnAuthenticationChallenge()
        {
            var wwwAuthenticate = new AuthenticationHeaderValue(AuthSchemes.ApiKey, "Provide an API Key");
            var ApiKey = "Key";

            var numberOfInvokes = 0;
            var numberOfRequests = 0;

            using var unauthorizeHandler = new HttpError401Handler((_, challenges, _) =>
            {
                Interlocked.Increment(ref numberOfInvokes);

                var authorization = challenges
                    .Where(x => x == wwwAuthenticate)
                    .Select(x => new AuthenticationHeaderValue(x.Scheme, ApiKey))
                    .FirstOrDefault();
                return Task.FromResult(authorization);
            });

            _client.ErrorHandlers.Add(unauthorizeHandler);

            _mockMessageHandler.MockHttpResponse(request =>
            {
                Interlocked.Increment(ref numberOfRequests);

                if (_client.DefaultRequestHeaders.Authorization is not null)
                    return new HttpResponseMessage(HttpStatusCode.OK);

                var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                response.Headers.WwwAuthenticate.Add(wwwAuthenticate);
                return response;
            });

            await _client.SendAsync(HttpMethod.Get, "/protected/resource");

            Assert.Multiple(() =>
            {
                Assert.That(numberOfRequests, Is.EqualTo(2));
                Assert.That(numberOfInvokes, Is.EqualTo(1));
                Assert.That(_client.DefaultRequestHeaders.Authorization, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Scheme, Is.EqualTo(wwwAuthenticate.Scheme));
                    Assert.That(_client.DefaultRequestHeaders.Authorization?.Parameter, Is.EqualTo(ApiKey));
                });
            });
        }
    }
}