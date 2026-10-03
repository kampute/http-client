namespace Kampute.HttpClient.Test
{
    using Kampute.Retry;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net.Http;

    [TestFixture]
    public class HttpRetryPolicyTests
    {
        private static HttpRequestErrorContext MockHttpRequestErrorContext()
        {
            var mockClient = new Mock<HttpRestClient>(new HttpClient(), true);
            var mockRequest = new Mock<HttpRequestMessage>();
            var mockError = new Mock<HttpRequestException>();

            return new HttpRequestErrorContext(mockClient.Object, mockRequest.Object, mockError.Object, new HttpRetryState());
        }

        [Test]
        public void CreateSession_StartsSessionForTheStrategy()
        {
            var mockRetryStrategy = new Mock<IRetryStrategy>();
            var policy = mockRetryStrategy.Object.ToHttpRetryPolicy();

            var first = policy.CreateSession(MockHttpRequestErrorContext()) as RetrySession;
            var second = policy.CreateSession(MockHttpRequestErrorContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.Not.Null);
                Assert.That(first?.Strategy, Is.SameAs(mockRetryStrategy.Object));
                Assert.That(second, Is.Not.SameAs(first));
            }
        }

        [Test]
        public void None_NeverRetries()
        {
            var session = HttpRetryPolicy.None.CreateSession(MockHttpRequestErrorContext());

            Assert.That(session.WaitAsync(default).Result, Is.False);
        }

        [Test]
        public void Dynamic_UsingStrategyFactory_CreatesSessionForTheStrategy()
        {
            var mockRetryStrategy = new Mock<IRetryStrategy>();
            var policy = HttpRetryPolicy.Dynamic(ctx => mockRetryStrategy.Object);

            var session = policy.CreateSession(MockHttpRequestErrorContext()) as RetrySession;

            Assert.That(session?.Strategy, Is.SameAs(mockRetryStrategy.Object));
        }

        [Test]
        public void Dynamic_UsingSessionFactory_ReturnsTheSession()
        {
            var mockRetrySession = new Mock<IRetrySession>();
            var policy = HttpRetryPolicy.Dynamic(ctx => mockRetrySession.Object);

            var session = policy.CreateSession(MockHttpRequestErrorContext());

            Assert.That(session, Is.SameAs(mockRetrySession.Object));
        }

        [Test]
        public void Dynamic_WhenFactoryReturnsNull_ThrowsInvalidOperationException()
        {
            var strategyPolicy = HttpRetryPolicy.Dynamic(ctx => (IRetryStrategy)null!);
            var sessionPolicy = HttpRetryPolicy.Dynamic(ctx => (IRetrySession)null!);

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<InvalidOperationException>(() => strategyPolicy.CreateSession(MockHttpRequestErrorContext()));
                Assert.Throws<InvalidOperationException>(() => sessionPolicy.CreateSession(MockHttpRequestErrorContext()));
            }
        }
    }
}
