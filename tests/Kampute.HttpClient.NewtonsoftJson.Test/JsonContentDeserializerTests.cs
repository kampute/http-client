namespace Kampute.HttpClient.NewtonsoftJson.Test
{
    using NUnit.Framework;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class JsonContentDeserializerTests
    {
        [Test]
        public void GetSupportedMediaTypes_ReturnsCorrectMediaTypes()
        {
            var deserializer = new JsonContentDeserializer();

            var supportedMediaTypes = deserializer.GetSupportedMediaTypes(typeof(TestModel));

            Assert.That(supportedMediaTypes, Contains.Item(MediaTypeNames.Application.Json));
        }

        [Test]
        public void CanDeserialize_ForSupportedMediaType_ReturnsTrue()
        {
            var deserializer = new JsonContentDeserializer();

            var canDeserialize = deserializer.CanDeserialize(MediaTypeNames.Application.Json, typeof(TestModel));

            Assert.That(canDeserialize, Is.True);
        }

        [Test]
        public void CanDeserialize_ForUnsupportedMediaType_ReturnsFalse()
        {
            var deserializer = new JsonContentDeserializer();

            var canDeserialize = deserializer.CanDeserialize(MediaTypeNames.Application.Xml, typeof(TestModel));

            Assert.That(canDeserialize, Is.False);
        }

        [Test]
        public async Task DeserializeAsync_WithUtf8EncodedJsonContent_ReturnsCorrectObject()
        {
            var expected = new TestModel { Name = "Test" };
            var content = new StringContent(expected.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var deserializer = new JsonContentDeserializer { Settings = TestModel.JsonSettings };

            var result = await deserializer.DeserializeAsync(content, typeof(TestModel)) as TestModel;

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task DeserializeAsync_WithNonUtf8EncodedJsonContent_ReturnsCorrectObject()
        {
            var expected = new TestModel { Name = "Test" };
            var content = new StringContent(expected.ToJsonString(), Encoding.BigEndianUnicode, MediaTypeNames.Application.Json);
            var deserializer = new JsonContentDeserializer { Settings = TestModel.JsonSettings };

            var result = await deserializer.DeserializeAsync(content, typeof(TestModel)) as TestModel;

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
