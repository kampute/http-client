namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.Content;
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class HttpContentFormatterCollectionTests
    {
        [Test]
        public void GetReaderFor_MediaTypeAndModelType_ReturnsCorrectFormatters()
        {
            var collection = new HttpContentFormatterCollection
            {
                new TestContentFormatter()
            };

            var result = collection.GetReaderFor(Constants.TestMediaType, typeof(string));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<TestContentFormatter>());
        }

        [Test]
        public void GetReaderFor_WhenFormatterMatches_CachesTheMatch()
        {
            var mockFormatter = new Mock<IHttpContentFormatter>();
            mockFormatter.Setup(d => d.CanRead(It.IsAny<string>(), It.IsAny<Type>())).Returns(true);
            var collection = new HttpContentFormatterCollection
            {
                mockFormatter.Object
            };

            var first = collection.GetReaderFor("application/test", typeof(string));
            var second = collection.GetReaderFor("application/test", typeof(string));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.SameAs(mockFormatter.Object));
                Assert.That(second, Is.SameAs(mockFormatter.Object));
            }
            mockFormatter.Verify(d => d.CanRead(It.IsAny<string>(), It.IsAny<Type>()), Times.Once);
        }

        [Test]
        public void GetReaderFor_WithMediaTypesDifferingOnlyInCase_SharesOneCacheEntry()
        {
            var mockFormatter = new Mock<IHttpContentFormatter>();
            mockFormatter.Setup(d => d.CanRead(It.IsAny<string>(), It.IsAny<Type>())).Returns(true);
            var collection = new HttpContentFormatterCollection
            {
                mockFormatter.Object
            };

            collection.GetReaderFor("application/test", typeof(string));
            var result = collection.GetReaderFor("Application/TEST", typeof(string));

            Assert.That(result, Is.SameAs(mockFormatter.Object));
            mockFormatter.Verify(d => d.CanRead(It.IsAny<string>(), It.IsAny<Type>()), Times.Once);
        }

        [Test]
        public void GetReaderFor_WhenNoFormatterMatches_DoesNotCacheTheMiss()
        {
            var mockFormatter = new Mock<IHttpContentFormatter>();
            mockFormatter.Setup(d => d.CanRead(It.IsAny<string>(), It.IsAny<Type>())).Returns(false);
            var collection = new HttpContentFormatterCollection
            {
                mockFormatter.Object
            };

            var first = collection.GetReaderFor("application/unknown", typeof(string));
            var second = collection.GetReaderFor("application/unknown", typeof(string));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.Null);
                Assert.That(second, Is.Null);
            }
            mockFormatter.Verify(d => d.CanRead(It.IsAny<string>(), It.IsAny<Type>()), Times.Exactly(2));
        }

        [Test]
        public void GetAcceptableMediaTypes_ModelType_ReturnsCorrectMediaTypeHeaderValues()
        {
            var modelType = typeof(string);
            var expectedMediaTypes = new[]
            {
                Constants.TestMediaType,
            };

            var collection = new HttpContentFormatterCollection
            {
                new TestContentFormatter()
            };

            var result = collection.GetAcceptableMediaTypes(modelType);

            Assert.That(result, Is.EqualTo(expectedMediaTypes));
        }

        [Test]
        public void GetAcceptableMediaTypes_ModelTypeAndErrorType_ReturnsCorrectMediaTypeHeaderValus()
        {
            var modelType = typeof(string);
            var errorType = typeof(object);
            var expectedMediaTypes = new[]
            {
                Constants.TestMediaType,
                MediaTypeNames.Application.Json,
            };

            var formatterMock = new Mock<IHttpContentFormatter>();
            formatterMock.Setup(formatter => formatter.GetReadableMediaTypes(It.IsAny<Type>()))
                .Returns((Type type) => type == typeof(string) ? [] : [MediaTypeNames.Application.Json]);

            var collection = new HttpContentFormatterCollection
            {
                formatterMock.Object,
                new TestContentFormatter(),
            };

            var result = collection.GetAcceptableMediaTypes(modelType, errorType);

            Assert.That(result, Is.EqualTo(expectedMediaTypes));
        }

        [Test]
        public void Add_Formatter_AddsFormatter()
        {
            var collection = new HttpContentFormatterCollection();

            collection.Add(new TestContentFormatter());

            Assert.That(collection, Has.Count.EqualTo(1));
        }

        [Test]
        public void Add_SameTypeFormatter_ThrowsArgumentException()
        {
            var collection = new HttpContentFormatterCollection
            {
                new TestContentFormatter()
            };

            var formatterSameType = new TestContentFormatter();

            Assert.Throws<ArgumentException>(() => collection.Add(formatterSameType));
        }

        [Test]
        public void Remove_WithExistingFormatter_RemovesFormatterAndReturnsTrue()
        {
            var formatter = new TestContentFormatter();
            var collection = new HttpContentFormatterCollection { formatter };

            var result = collection.Remove(formatter);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(collection, Is.Empty);
            }
        }

        [Test]
        public void Remove_WithNonExistingFormatter_ReturnsFalse()
        {
            var collection = new HttpContentFormatterCollection();
            var formatter = new TestContentFormatter();

            var result = collection.Remove(formatter);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Contains_WithExistingFormatter_ReturnsTrue()
        {
            var formatter = new TestContentFormatter();
            var collection = new HttpContentFormatterCollection { formatter };

            var result = collection.Contains(formatter);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Contains_WithNonExistingFormatter_ReturnsFalse()
        {
            var collection = new HttpContentFormatterCollection();
            var formatter = new TestContentFormatter();

            var result = collection.Contains(formatter);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Clear_ResetsCollection()
        {
            var formatter = new TestContentFormatter();
            var collection = new HttpContentFormatterCollection { formatter };

            collection.Clear();

            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void Find_WithExistingFormatterType_ReturnsFormatter()
        {
            var formatter = new TestContentFormatter();
            var collection = new HttpContentFormatterCollection { formatter };

            var result = collection.Find<TestContentFormatter>();

            Assert.That(result, Is.EqualTo(formatter));
        }

        [Test]
        public void Find_WithNonExistingFormatterType_ReturnsNull()
        {
            var collection = new HttpContentFormatterCollection();

            var result = collection.Find<TestContentFormatter>();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindOrDefault_WithExistingFormatterType_ReturnsRegisteredInstance()
        {
            var formatter = new FormUrlEncodedFormatter();
            var collection = new HttpContentFormatterCollection { formatter };

            var result = collection.FindOrDefault<FormUrlEncodedFormatter>();

            Assert.That(result, Is.SameAs(formatter));
        }

        [Test]
        public void FindOrDefault_WithNonExistingFormatterType_ReturnsNewUnregisteredInstanceEachTime()
        {
            var collection = new HttpContentFormatterCollection();

            var first = collection.FindOrDefault<FormUrlEncodedFormatter>();
            var second = collection.FindOrDefault<FormUrlEncodedFormatter>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.Not.Null);
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(collection, Is.Empty);
            }
        }

        [Test]
        public void GetWriterFor_WithMediaTypeInDifferentCase_ReturnsMatchingFormatter()
        {
            var collection = new HttpContentFormatterCollection { new FormUrlEncodedFormatter() };

            var result = collection.GetWriterFor("Application/X-WWW-Form-UrlEncoded", typeof(Dictionary<string, string>));

            Assert.That(result, Is.TypeOf<FormUrlEncodedFormatter>());
        }

        [Test]
        public void GetWriterFor_WithSeveralMatchingFormatters_ReturnsFirstRegistered()
        {
            var mockWriter = MockWriter(true).Object;
            var testFormatter = new TestContentFormatter();
            var mockFirst = new HttpContentFormatterCollection { mockWriter, testFormatter };
            var testFirst = new HttpContentFormatterCollection { testFormatter, mockWriter };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mockFirst.GetWriterFor(Constants.TestMediaType, typeof(string)), Is.SameAs(mockWriter));
                Assert.That(testFirst.GetWriterFor(Constants.TestMediaType, typeof(string)), Is.SameAs(testFormatter));
            }
        }

        [Test]
        public void GetWriterFor_WhenFormatterMatches_CachesTheMatchIgnoringCase()
        {
            var writer = MockWriter(true);
            var collection = new HttpContentFormatterCollection { writer.Object };

            var first = collection.GetWriterFor("application/known", typeof(string));
            var second = collection.GetWriterFor("APPLICATION/KNOWN", typeof(string));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.SameAs(writer.Object));
                Assert.That(second, Is.SameAs(writer.Object));
            }
            writer.Verify(f => f.CanWrite(It.IsAny<string>(), It.IsAny<Type>()), Times.Once);
        }

        [Test]
        public void GetWriterFor_WhenNoFormatterMatches_DoesNotCacheTheMiss()
        {
            var writer = MockWriter(false);
            var collection = new HttpContentFormatterCollection { writer.Object };

            var first = collection.GetWriterFor("application/unknown", typeof(string));
            var second = collection.GetWriterFor("application/unknown", typeof(string));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.Null);
                Assert.That(second, Is.Null);
            }
            writer.Verify(f => f.CanWrite(It.IsAny<string>(), It.IsAny<Type>()), Times.Exactly(2));
        }

        [Test]
        public void GetWriterFor_DoesNotUseReadingSide()
        {
            var collection = new HttpContentFormatterCollection { new TestContentFormatter() };

            var reader = collection.GetReaderFor(Constants.TestMediaType, typeof(string));
            var writer = collection.GetWriterFor(MediaTypeNames.Application.FormUrlEncoded, typeof(Dictionary<string, string>));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reader, Is.Not.Null);
                Assert.That(writer, Is.Null);
            }
        }

        [Test]
        public void GetReaderFor_WithSendOnlyFormatter_ReturnsNull()
        {
            var collection = new HttpContentFormatterCollection { new FormUrlEncodedFormatter() };

            var result = collection.GetReaderFor(MediaTypeNames.Application.FormUrlEncoded, typeof(Dictionary<string, string>));

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetAcceptableMediaTypes_WithSendOnlyFormatter_AddsNothing()
        {
            var collection = new HttpContentFormatterCollection { new FormUrlEncodedFormatter(), new TestContentFormatter() };

            var result = collection.GetAcceptableMediaTypes(typeof(string));

            Assert.That(result, Is.EqualTo([Constants.TestMediaType]));
        }

        [Test]
        public void GetReaderFor_WithNullArgument_ThrowsArgumentNullException()
        {
            var collection = new HttpContentFormatterCollection();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentNullException>(() => collection.GetReaderFor(null!, typeof(string)));
                Assert.Throws<ArgumentNullException>(() => collection.GetReaderFor(Constants.TestMediaType, null!));
                Assert.Throws<ArgumentNullException>(() => collection.GetWriterFor(null!, typeof(string)));
                Assert.Throws<ArgumentNullException>(() => collection.GetWriterFor(Constants.TestMediaType, null!));
            }
        }

        private static Mock<IHttpContentFormatter> MockWriter(bool canWrite)
        {
            var writer = new Mock<IHttpContentFormatter>();
            writer.Setup(f => f.CanWrite(It.IsAny<string>(), It.IsAny<Type>())).Returns(canWrite);
            return writer;
        }
    }
}
