namespace Kampute.HttpClient.NetFramework.Test
{
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.Resilience;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    [TestFixture]
    public class TargetSpecificBehaviorTests
    {
        [Test]
        public async Task On429Response_IsHandledByHttpError429Handler()
        {
            using var handler = new TestHttpMessageHandler(request => request.GetCloneGeneration() == 0
                ? new HttpResponseMessage((HttpStatusCode)429)
                : new HttpResponseMessage(HttpStatusCode.OK));
            using var client = CreateClient(handler);
            client.ErrorHandlers.Add(new HttpError429Handler
            {
                OnRetryPolicy = (_, _) => RetryStrategies.Constant(TimeSpan.Zero).WithMaxRetries(1).ToHttpRetryPolicy()
            });

            using var response = await client.SendAsync(HttpMethod.Get, "/rate-limited/resource");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(handler.Attempts, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task HttpVerbPatch_SendsPatchMethod()
        {
            var sentMethod = default(string);
            using var handler = new TestHttpMessageHandler(request =>
            {
                sentMethod = request.Method.Method;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            using var client = CreateClient(handler);

            using var response = await client.SendAsync(HttpVerb.Patch, "/resource");

            Assert.That(sentMethod, Is.EqualTo("PATCH"));
        }

        [Test]
        public void PostAsFormAsync_OnErrorResponse_ThrowsHttpResponseException()
        {
            using var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            using var client = CreateClient(handler);

            var exception = Assert.ThrowsAsync<HttpResponseException>(() => client.PostAsFormAsync("/resource", [new KeyValuePair<string, string>("name", "value")]));

            Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public void OnErrorResponse_ResponseMessageKeepsHeadersButContentIsDisposed()
        {
            using var handler = new TestHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Error details") };
                response.Headers.Add("X-Error-Id", "42");
                return response;
            });
            using var client = CreateClient(handler);

            var exception = Assert.ThrowsAsync<HttpResponseException>(() => client.SendAsync(HttpMethod.Get, "/resource"));

            Assert.That(exception.ResponseMessage, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ResponseMessage!.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(exception.ResponseMessage.Headers.GetValues("X-Error-Id"), Is.EqualTo(new[] { "42" }));
                Assert.ThrowsAsync<ObjectDisposedException>(() => exception.ResponseMessage.Content.ReadAsStringAsync());
            }
        }

        [Test]
        public async Task GetToStreamAsync_WithBodyLargerThanBufferLimit_StreamsBody()
        {
            var body = new byte[1024];
            new Random(1).NextBytes(body);

            using var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
            using var client = new HttpRestClient(new System.Net.Http.HttpClient(handler, disposeHandler: false) { MaxResponseContentBufferSize = 16 })
            {
                BaseAddress = new Uri("http://api.test.com"),
            };

            using var resultStream = new System.IO.MemoryStream();
            await client.GetToStreamAsync("/resource", resultStream);

            Assert.That(resultStream.ToArray(), Is.EqualTo(body));
        }

        private static HttpRestClient CreateClient(TestHttpMessageHandler handler)
        {
            return new HttpRestClient(new System.Net.Http.HttpClient(handler, disposeHandler: false))
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
        }
    }
}
