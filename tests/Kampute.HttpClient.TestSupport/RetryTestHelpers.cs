namespace Kampute.HttpClient.TestSupport
{
    using Kampute.HttpClient.Interfaces;
    using Moq;
    using System.Threading;

    public static class RetryTestHelpers
    {
        public static Mock<IHttpBackoffProvider> MockBackoffStrategy(int retriesToAllow, out Mock<IRetryScheduler> mockRetryScheduler)
        {
            mockRetryScheduler = new Mock<IRetryScheduler>();

            var retries = 0;
            mockRetryScheduler.Setup(scheduler => scheduler.WaitAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => retries < retriesToAllow)
                .Callback(() => ++retries);

            var mockBackoffStrategy = new Mock<IHttpBackoffProvider>();
            mockBackoffStrategy.Setup(strategy => strategy.CreateScheduler(It.IsAny<HttpRequestErrorContext>()))
                .Returns(mockRetryScheduler.Object);

            return mockBackoffStrategy;
        }
    }
}
