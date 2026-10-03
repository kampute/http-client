namespace Kampute.HttpClient.NetFramework.Test
{
    using Kampute.HttpClient.ErrorHandlers;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    [TestFixture]
    public class RetryWithContentTests
    {
        private const string Payload = "test payload";

        [Test]
        public async Task OnConnectionFailure_WithStringContent_RetriesWithSameBody()
        {
            using var handler = new TestHttpMessageHandler(request =>
            {
                Assert.That(TestHttpMessageHandler.ReadContent(request.Content!), Is.EqualTo(Payload));
                return Attempt(request) == 1 ? throw ConnectionFailure() : new HttpResponseMessage(HttpStatusCode.OK);
            });
            using var client = CreateClient(handler);
            client.BackoffStrategy = BackoffStrategies.Uniform(1, TimeSpan.Zero);

            using var content = new StringContent(Payload);
            using var response = await client.SendAsync(HttpMethod.Post, "/resource", content);

            Assert.That(handler.Attempts, Is.EqualTo(2));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public async Task OnConnectionFailure_WithCompressedContent_RetriesWithSameBody(string encoding)
        {
            using var handler = new TestHttpMessageHandler(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content!.Headers.ContentEncoding, Does.Contain(encoding));
                    Assert.That(TestHttpMessageHandler.ReadContent(request.Content!), Is.EqualTo(Payload));
                }
                return Attempt(request) == 1 ? throw ConnectionFailure() : new HttpResponseMessage(HttpStatusCode.OK);
            });
            using var client = CreateClient(handler);
            client.BackoffStrategy = BackoffStrategies.Uniform(1, TimeSpan.Zero);

            using var content = new StringContent(Payload);
            using var compressedContent = encoding == "gzip" ? (HttpContent)content.AsGzip() : content.AsDeflate();
            using var response = await client.SendAsync(HttpMethod.Post, "/resource", compressedContent);

            Assert.That(handler.Attempts, Is.EqualTo(2));
        }

        [Test]
        public async Task On401Response_WithStringContent_RetriesPostWithSameBody()
        {
            var authorization = new AuthenticationHeaderValue(AuthSchemes.Bearer, "token");

            using var handler = new TestHttpMessageHandler(request =>
            {
                Assert.That(TestHttpMessageHandler.ReadContent(request.Content!), Is.EqualTo(Payload));
                return authorization.Equals(request.Headers.Authorization)
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    : new HttpResponseMessage(HttpStatusCode.Unauthorized);
            });
            using var client = CreateClient(handler);
            using var unauthorizedHandler = new HttpError401Handler((_, _) => Task.FromResult<AuthenticationHeaderValue?>(authorization));
            client.ErrorHandlers.Add(unauthorizedHandler);

            using var content = new StringContent(Payload);
            using var response = await client.SendAsync(HttpMethod.Post, "/resource", content);

            Assert.That(handler.Attempts, Is.EqualTo(2));
        }

        private static HttpRestClient CreateClient(TestHttpMessageHandler handler)
        {
            return new HttpRestClient(new System.Net.Http.HttpClient(handler, disposeHandler: false))
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
        }

        private static int Attempt(HttpRequestMessage request) => request.GetCloneGeneration() + 1;

        private static HttpRequestException ConnectionFailure()
        {
            return new HttpRequestException("Connection failure", new SocketException((int)SocketError.HostUnreachable));
        }
    }
}
