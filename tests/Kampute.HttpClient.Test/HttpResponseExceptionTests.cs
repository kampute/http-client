namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpResponseExceptionTests
    {
        private static TestCaseData[] Constructors() =>
        [
            new TestCaseData(new HttpResponseException(HttpStatusCode.NotFound)).SetName("Constructor with status code"),
            new TestCaseData(new HttpResponseException(HttpStatusCode.NotFound, "Not found")).SetName("Constructor with message"),
            new TestCaseData(new HttpResponseException(HttpStatusCode.NotFound, "Not found", new InvalidOperationException())).SetName("Constructor with inner exception"),
        ];

        [TestCaseSource(nameof(Constructors))]
        public void BaseStatusCode_MatchesStatusCode(HttpResponseException exception)
        {
            HttpRequestException baseException = exception;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(baseException.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task ExceptionFilterOnBaseStatusCode_MatchesErrorResponse()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.MockHttpResponse(HttpStatusCode.NotFound);
            using var client = new HttpRestClient(new HttpClient(handler.Object))
            {
                BaseAddress = new Uri("http://api.test.com"),
            };

            var matched = false;
            try
            {
                using var _ = await client.SendAsync(HttpMethod.Get, "/missing");
            }
            catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.NotFound)
            {
                matched = true;
            }

            Assert.That(matched, Is.True);
        }
    }
}
