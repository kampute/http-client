namespace Kampute.HttpClient.Xml.Test
{
    using NUnit.Framework;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class XmlContentDeserializerTests
    {
        [Test]
        public void GetSupportedMediaTypes_ForNonNullModelType_ReturnsCorrectMediaTypes()
        {
            var deserializer = new XmlContentDeserializer();

            var supportedMediaTypes = deserializer.GetSupportedMediaTypes(typeof(TestModel));

            Assert.That(supportedMediaTypes, Contains.Item(MediaTypeNames.Application.Xml));
        }

        [Test]
        public void GetSupportedMediaTypes_ForNullModelType_ReturnsEmpty()
        {
            var deserializer = new XmlContentDeserializer();

            var supportedMediaTypes = deserializer.GetSupportedMediaTypes(null);

            Assert.That(supportedMediaTypes, Is.Empty);
        }

        [Test]
        public void CanDeserialize_ForSupportedMediaType_ReturnsTrue()
        {
            var deserializer = new XmlContentDeserializer();

            var canDeserialize = deserializer.CanDeserialize(MediaTypeNames.Application.Xml, typeof(TestModel));

            Assert.That(canDeserialize, Is.True);
        }

        [Test]
        public void CanDeserialize_ForUnsupportedMediaType_ReturnsFalse()
        {
            var deserializer = new XmlContentDeserializer();

            var canDeserialize = deserializer.CanDeserialize(MediaTypeNames.Application.Json, typeof(TestModel));

            Assert.That(canDeserialize, Is.False);
        }

        [Test]
        public void CanDeserialize_ForNullModelType_ReturnsFalse()
        {
            var deserializer = new XmlContentDeserializer();

            var canDeserialize = deserializer.CanDeserialize(MediaTypeNames.Application.Xml, null);

            Assert.That(canDeserialize, Is.False);
        }

        [Test]
        public async Task DeserializeAsync_WithUtf8EncodedXmlContent_ReturnsCorrectObject()
        {
            var encoding = Encoding.UTF8;
            var expected = new TestModel { Name = "Test" };
            var content = new StringContent(expected.ToXmlString(encoding), encoding, MediaTypeNames.Application.Xml);
            var deserializer = new XmlContentDeserializer();

            var result = await deserializer.DeserializeAsync(content, typeof(TestModel)) as TestModel;

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task DeserializeAsync_WithNonUtf8EncodedXmlContent_ReturnsCorrectObject()
        {
            var encoding = Encoding.BigEndianUnicode;
            var expected = new TestModel { Name = "Test" };
            var content = new StringContent(expected.ToXmlString(encoding), encoding, MediaTypeNames.Application.Xml);
            var deserializer = new XmlContentDeserializer();

            var result = await deserializer.DeserializeAsync(content, typeof(TestModel)) as TestModel;

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
