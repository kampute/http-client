namespace Kampute.HttpClient.Xml.Test
{
    using NUnit.Framework;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class XmlContentTests
    {
        [Test]
        public async Task WithDefaultEncoding_SetsContentCorrectly()
        {
            var model = new TestModel { Name = "Test" };
            var expectedString = model.ToXmlString(Encoding.UTF8);

            using var xmlContent = new XmlContent(model);

            var xmlString = await xmlContent.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(xmlString, Is.EqualTo(expectedString));
                Assert.That(xmlContent.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                Assert.That(xmlContent.Headers.ContentType?.CharSet, Is.EqualTo(Encoding.UTF8.WebName));
            });
        }

        [Test]
        public async Task WithCustomEncoding_SetsContentCorrectly()
        {
            var encoding = Encoding.BigEndianUnicode;
            var model = new TestModel { Name = "Test" };
            var expectedString = model.ToXmlString(encoding);

            using var xmlContent = new XmlContent(model, encoding);

            var xmlString = await xmlContent.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(xmlString, Is.EqualTo(expectedString));
                Assert.That(xmlContent.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                Assert.That(xmlContent.Headers.ContentType?.CharSet, Is.EqualTo(encoding.WebName));
            });
        }
    }
}
