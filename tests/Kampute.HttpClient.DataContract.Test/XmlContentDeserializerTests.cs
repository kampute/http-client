namespace Kampute.HttpClient.DataContract.Test
{
    using NUnit.Framework;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class XmlContentDeserializerTests
    {
        [Test]
        public void GetSupportedMediaTypes_ReturnsCorrectMediaTypes()
        {
            var deserializer = new XmlContentDeserializer();

            var supportedMediaTypes = deserializer.GetReadableMediaTypes(typeof(TestModel));

            Assert.That(supportedMediaTypes, Contains.Item(MediaTypeNames.Application.Xml));
        }

        [Test]
        public void CanDeserialize_ForSupportedMediaType_ReturnsTrue()
        {
            var deserializer = new XmlContentDeserializer();

            var canDeserialize = deserializer.CanRead(MediaTypeNames.Application.Xml, typeof(TestModel));

            Assert.That(canDeserialize, Is.True);
        }

        [Test]
        public void CanDeserialize_ForSupportedMediaTypeInDifferentCase_ReturnsTrue()
        {
            var deserializer = new XmlContentDeserializer();

            var canDeserialize = deserializer.CanRead("Application/XML", typeof(TestModel));

            Assert.That(canDeserialize, Is.True);
        }

        [Test]
        public void CanDeserialize_ForUnsupportedMediaType_ReturnsFalse()
        {
            var deserializer = new XmlContentDeserializer();

            var canDeserialize = deserializer.CanRead(MediaTypeNames.Application.Json, typeof(TestModel));

            Assert.That(canDeserialize, Is.False);
        }

        [Test]
        public async Task DeserializeAsync_WithUtf8EncodedXmlContent_ReturnsCorrectObject()
        {
            var encoding = Encoding.UTF8;
            var expected = new TestModel { Name = "Test" };
            var content = new StringContent(expected.ToXmlString(encoding), encoding, MediaTypeNames.Application.Xml);
            var deserializer = new XmlContentDeserializer();

            var result = await deserializer.ReadAsync(content, typeof(TestModel)) as TestModel;

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task DeserializeAsync_WithNonUtf8EncodedXmlContent_ReturnsCorrectObject()
        {
            var encoding = Encoding.BigEndianUnicode;
            var expected = new TestModel { Name = "Test" };
            var content = new StringContent(expected.ToXmlString(encoding), encoding, MediaTypeNames.Application.Xml);
            var deserializer = new XmlContentDeserializer();

            var result = await deserializer.ReadAsync(content, typeof(TestModel)) as TestModel;

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
