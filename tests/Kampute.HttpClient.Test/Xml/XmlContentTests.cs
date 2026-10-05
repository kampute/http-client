namespace Kampute.HttpClient.Test.Xml
{
    using Kampute.HttpClient.Xml;
    using NUnit.Framework;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class XmlContentTests
    {
        [Test]
        public async Task WithXmlSerializer_AndDefaultEncoding_SetsContentCorrectly()
        {
            var model = new PlainModel { Name = "Test" };

            using var xmlContent = new XmlContent(model) { Serializer = XmlSerializerKind.XmlSerializer };

            await AssertContent(xmlContent, model.ToXmlSerializerString(Encoding.UTF8), Encoding.UTF8);
        }

        [Test]
        public async Task WithXmlSerializer_AndCustomEncoding_SetsContentCorrectly()
        {
            var encoding = Encoding.BigEndianUnicode;
            var model = new PlainModel { Name = "Test" };

            using var xmlContent = new XmlContent(model, encoding) { Serializer = XmlSerializerKind.XmlSerializer };

            await AssertContent(xmlContent, model.ToXmlSerializerString(encoding), encoding);
        }

        [Test]
        public async Task WithDataContractSerializer_AndDefaultEncoding_SetsContentCorrectly()
        {
            var model = new ContractModel { Name = "Test" };

            using var xmlContent = new XmlContent(model) { Serializer = XmlSerializerKind.DataContractSerializer };

            await AssertContent(xmlContent, model.ToDataContractString(Encoding.UTF8), Encoding.UTF8);
        }

        [Test]
        public async Task WithDataContractSerializer_AndCustomEncoding_SetsContentCorrectly()
        {
            var encoding = Encoding.BigEndianUnicode;
            var model = new ContractModel { Name = "Test" };

            using var xmlContent = new XmlContent(model, encoding) { Serializer = XmlSerializerKind.DataContractSerializer };

            await AssertContent(xmlContent, model.ToDataContractString(encoding), encoding);
        }

        [Test]
        public async Task WithAuto_ChoosesSerializerByPayloadType()
        {
            using var plainContent = new XmlContent(new PlainModel { Name = "Test" });
            using var contractContent = new XmlContent(new ContractModel { Name = "Test" });
            using var dualContent = new XmlContent(new DualModel { Name = "Test" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await plainContent.ReadAsStringAsync(), Does.Not.Contain(XmlStrings.DataContractNamespacePrefix));
                Assert.That(await contractContent.ReadAsStringAsync(), Does.Contain(XmlStrings.DataContractNamespacePrefix));
                Assert.That(await dualContent.ReadAsStringAsync(), Does.Contain(XmlStrings.DataContractNamespacePrefix));
            }
        }

        private static async Task AssertContent(XmlContent xmlContent, string expectedString, Encoding encoding)
        {
            var xmlString = await xmlContent.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(xmlString, Is.EqualTo(expectedString));
                Assert.That(xmlContent.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Xml));
                Assert.That(xmlContent.Headers.ContentType?.CharSet, Is.EqualTo(encoding.WebName));
            }
        }
    }
}
