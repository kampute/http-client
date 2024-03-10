﻿namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net.Http.Headers;

    [TestFixture]
    public class HttpContentDeserializerCollectionTests
    {
        [Test]
        public void For_MediaTypeAndModelType_ReturnsCorrectDeserializers()
        {
            var collection = new HttpContentDeserializerCollection
            {
                new TestContentDeserializer()
            };

            var result = collection.For(Constants.TestMediaType, typeof(string));

            Assert.That(result, Has.Exactly(1).Items);
            Assert.That(result.First(), Is.TypeOf<TestContentDeserializer>());
        }

        [Test]
        public void GetSupportedMediaTypes_ModelType_ReturnsCorrectMediaTypes()
        {
            var modelType = typeof(string);

            var collection = new HttpContentDeserializerCollection
            {
                new TestContentDeserializer()
            };

            var result = collection.GetSupportedMediaTypes(modelType);

            Assert.That(result, Is.EqualTo(new[] { new MediaTypeWithQualityHeaderValue(Constants.TestMediaType) }));
        }

        [Test]
        public void GetSupportedMediaTypes_ModelTypeAndErrorType_ReturnsCorrectMediaTypes()
        {
            var modelType = typeof(string);
            var errorType = typeof(object);
            var expectedMediaTypes = new[]
            {
                new MediaTypeWithQualityHeaderValue(Constants.TestMediaType),
                new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json),
            };

            var deserializerMock = new Mock<IHttpContentDeserializer>();
            deserializerMock.Setup(deserializer => deserializer.GetSupportedMediaTypes(It.IsAny<Type>()))
                .Returns(new[] { MediaTypeNames.Application.Json });

            var collection = new HttpContentDeserializerCollection
            {
                deserializerMock.Object,
                new TestContentDeserializer(),
            };

            var result = collection.GetSupportedMediaTypes(modelType, errorType);

            Assert.That(result, Is.EquivalentTo(expectedMediaTypes));
        }

        [Test]
        public void Add_Deserializer_AddsDeserializer()
        {
            var collection = new HttpContentDeserializerCollection();

            collection.Add(new TestContentDeserializer());

            Assert.That(collection, Has.Count.EqualTo(1));
        }

        [Test]
        public void Add_SameTypeDeserializer_ThrowsArgumentException()
        {
            var collection = new HttpContentDeserializerCollection
            {
                new TestContentDeserializer()
            };

            var deserializerSameType = new TestContentDeserializer();

            Assert.Throws<ArgumentException>(() => collection.Add(deserializerSameType));
        }

        [Test]
        public void Remove_WithExistingDeserializer_RemovesDeserializerAndReturnsTrue()
        {
            var deserializer = new TestContentDeserializer();
            var collection = new HttpContentDeserializerCollection { deserializer };

            var result = collection.Remove(deserializer);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(collection, Is.Empty);
            });
        }

        [Test]
        public void Remove_WithNonExistingDeserializer_ReturnsFalse()
        {
            var collection = new HttpContentDeserializerCollection();
            var deserializer = new TestContentDeserializer();

            var result = collection.Remove(deserializer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Contains_WithExistingDeserializer_ReturnsTrue()
        {
            var deserializer = new TestContentDeserializer();
            var collection = new HttpContentDeserializerCollection { deserializer };

            var result = collection.Contains(deserializer);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Contains_WithNonExistingDeserializer_ReturnsFalse()
        {
            var collection = new HttpContentDeserializerCollection();
            var deserializer = new TestContentDeserializer();

            var result = collection.Contains(deserializer);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Clear_ResetsCollection()
        {
            var deserializer = new TestContentDeserializer();
            var collection = new HttpContentDeserializerCollection { deserializer };

            collection.Clear();

            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void Find_WithExistingDeserializerType_ReturnsDeserializer()
        {
            var deserializer = new TestContentDeserializer();
            var collection = new HttpContentDeserializerCollection { deserializer };

            var result = collection.Find<TestContentDeserializer>();

            Assert.That(result, Is.EqualTo(deserializer));
        }

        [Test]
        public void Find_WithNonExistingDeserializerType_ReturnsNull()
        {
            var collection = new HttpContentDeserializerCollection();

            var result = collection.Find<TestContentDeserializer>();

            Assert.That(result, Is.Null);
        }
    }
}