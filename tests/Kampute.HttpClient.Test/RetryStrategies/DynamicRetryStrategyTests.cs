namespace Kampute.HttpClient.Test.RetryStrategies
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryStrategies;
    using Moq;
    using NUnit.Framework;
    using System.Net.Http;

    [TestFixture]
    public class DynamicRetryStrategyTests
    {
        private static HttpRequestErrorContext MockHttpRequestErrorContext()
        {
            var mockClient = new Mock<HttpRestClient>();
            var mockRequest = new Mock<HttpRequestMessage>();
            var mockError = new Mock<HttpRequestException>();

            return new HttpRequestErrorContext(mockClient.Object, mockRequest.Object, mockError.Object);
        }

        [Test]
        public void CreateScheduler_UsingStrategyFactory_ReturnsCorrectScheduler()
        {
            var mockRetryStrategy = new Mock<IRetryStrategy>();
            var mockRetryScheduler = new Mock<IRetryScheduler>();
            mockRetryStrategy.Setup(x => x.CreateScheduler(It.IsAny<HttpRequestErrorContext>()))
                             .Returns(mockRetryScheduler.Object);
            var strategy = new DynamicRetryStrategy(ctx => mockRetryStrategy.Object);
            var context = MockHttpRequestErrorContext();

            var scheduler = strategy.CreateScheduler(context);

            Assert.That(mockRetryScheduler.Object, Is.SameAs(scheduler));
        }

        [Test]
        public void CreateScheduler_UsingSchedulerFactory_ReturnsCorrectScheduler()
        {
            var mockRetryScheduler = new Mock<IRetryScheduler>();
            var strategy = new DynamicRetryStrategy(ctx => mockRetryScheduler.Object);
            var context = MockHttpRequestErrorContext();

            var scheduler = strategy.CreateScheduler(context);

            Assert.That(mockRetryScheduler.Object, Is.SameAs(scheduler));
        }
    }
}
