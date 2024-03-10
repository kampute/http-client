namespace Kampute.HttpClient.Test.RetryManagement
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetryManagement;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class RetrySchedulerFactoryTests
    {
        [Test]
        public void CreatesSchedulerWithCorrectStrategy()
        {
            var mockRetryStrategy = new Mock<IRetryStrategy>();
            var factory = new RetrySchedulerFactory(mockRetryStrategy.Object);

            var scheduler = factory.CreateScheduler() as RetryScheduler;

            Assert.That(scheduler, Is.Not.Null);
            Assert.That(scheduler.Strategy, Is.SameAs(mockRetryStrategy.Object));
        }
    }
}
