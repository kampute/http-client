namespace Kampute.HttpClient.NetFramework.Test
{
    using Kampute.HttpClient.ErrorHandlers;
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
                OnBackoffStrategy = (_, _) => BackoffStrategies.Uniform(1, TimeSpan.Zero)
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

        private static HttpRestClient CreateClient(TestHttpMessageHandler handler)
        {
            return new HttpRestClient(new System.Net.Http.HttpClient(handler, disposeHandler: false))
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
        }
    }
}
