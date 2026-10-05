namespace Kampute.HttpClient.TestSupport
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.Resilience;
    using Moq;
    using System.Threading;

    public static class RetryTestHelpers
    {
        public static Mock<IHttpRetryPolicy> MockRetryPolicy(int retriesToAllow, out Mock<IRetrySession> mockRetrySession)
        {
            mockRetrySession = new Mock<IRetrySession>();

            var retries = 0;
            mockRetrySession.Setup(session => session.WaitToRetryAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => retries < retriesToAllow)
                .Callback(() => ++retries);

            var mockRetryPolicy = new Mock<IHttpRetryPolicy>();
            mockRetryPolicy.Setup(strategy => strategy.CreateSession(It.IsAny<HttpRequestErrorContext>()))
                .Returns(mockRetrySession.Object);

            return mockRetryPolicy;
        }
    }
}
