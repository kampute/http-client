namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.Interfaces;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;

    [TestFixture]
    public class HttpErrorHandlerCollectionTests
    {
        [Test]
        public void For_StatusCode_ReturnsCorrectErrorHandlers()
        {
            var statusCode = HttpStatusCode.Unauthorized;
            var collection = new HttpErrorHandlerCollection();
            for (var i = 0; i < 5; i++)
            {
                var errorHandlerMock = new Mock<IHttpErrorHandler>();
                errorHandlerMock.Setup(errorHandler => errorHandler.CanHandle(statusCode)).Returns(i % 2 == 0);
                collection.Add(errorHandlerMock.Object);
            }

            var result = collection.For(statusCode);

            Assert.That(result, Has.Exactly(3).Items);
        }

        [Test]
        public void Add_WithNonExistingErrorHandler_AddsErrorHandler()
        {
            var collection = new HttpErrorHandlerCollection();

            collection.Add(new Mock<IHttpErrorHandler>().Object);

            Assert.That(collection, Has.Count.EqualTo(1));
        }

        [Test]
        public void Add_WithExistingErrorHandler_ThrowsArgumentException()
        {
            var errorHandler = new Mock<IHttpErrorHandler>().Object;
            var collection = new HttpErrorHandlerCollection { errorHandler };

            Assert.Throws<ArgumentException>(() => collection.Add(errorHandler));
        }

        [Test]
        public void Remove_WithExistingErrorHandler_RemovesErrorHandlerAndReturnsTrue()
        {
            var errorHandler = new Mock<IHttpErrorHandler>().Object;
            var collection = new HttpErrorHandlerCollection { errorHandler };

            var result = collection.Remove(errorHandler);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(collection, Is.Empty);
            });
        }

        [Test]
        public void Remove_WithNonExistingErrorHandler_ReturnsFalse()
        {
            var collection = new HttpErrorHandlerCollection();
            var errorHandler = new Mock<IHttpErrorHandler>().Object;

            var result = collection.Remove(errorHandler);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Contains_WithExistingErrorHandler_ReturnsTrue()
        {
            var errorHandler = new Mock<IHttpErrorHandler>().Object;
            var collection = new HttpErrorHandlerCollection { errorHandler };

            var result = collection.Contains(errorHandler);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Contains_WithNonExistingErrorHandler_ReturnsFalse()
        {
            var collection = new HttpErrorHandlerCollection();
            var errorHandler = new Mock<IHttpErrorHandler>().Object;

            var result = collection.Contains(errorHandler);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Clear_ResetsCollection()
        {
            var errorHandler = new Mock<IHttpErrorHandler>().Object;
            var collection = new HttpErrorHandlerCollection { errorHandler };

            collection.Clear();

            Assert.That(collection, Is.Empty);
        }
    }
}
