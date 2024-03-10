namespace Kampute.HttpClient.Test.RetryManagement
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement;
    using Moq;
    using NUnit.Framework;
    using System.Net.Http;

    [TestFixture]
    public class DynamicRetrySchedulerFactoryTests
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
            var factory = new DynamicRetrySchedulerFactory(ctx => mockRetryStrategy.Object);
            var context = MockHttpRequestErrorContext();

            var scheduler = factory.CreateScheduler(context) as RetryScheduler;

            Assert.That(scheduler, Is.Not.Null);
            Assert.That(scheduler.Strategy, Is.SameAs(mockRetryStrategy.Object));
        }

        [Test]
        public void CreateScheduler_UsingSchedulerFactory_ReturnsCorrectScheduler()
        {
            var mockRetryScheduler = new Mock<IRetryScheduler>();
            var factory = new DynamicRetrySchedulerFactory(ctx => mockRetryScheduler.Object);
            var context = MockHttpRequestErrorContext();

            var scheduler = factory.CreateScheduler(context);

            Assert.That(scheduler, Is.SameAs(mockRetryScheduler.Object));
        }
    }
}
