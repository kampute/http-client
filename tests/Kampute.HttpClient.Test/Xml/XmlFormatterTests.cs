namespace Kampute.HttpClient.Test.Xml
{
    using Kampute.HttpClient.Xml;
    using NUnit.Framework;
    using System;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    [TestFixture]
    public class XmlFormatterTests
    {
        [Test]
        public void Serializer_DefaultsToAuto()
        {
            Assert.That(new XmlFormatter().Serializer, Is.EqualTo(XmlSerializerKind.Auto));
        }

        [Test]
        public void MediaTypes_AreApplicationXmlInBothDirections()
        {
            var formatter = new XmlFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formatter.GetReadableMediaTypes(typeof(PlainModel)), Is.EqualTo(new[] { MediaTypeNames.Application.Xml }));
                Assert.That(formatter.GetWritableMediaTypes(typeof(PlainModel)), Is.EqualTo(new[] { MediaTypeNames.Application.Xml }));
                Assert.That(formatter.CanRead(MediaTypeNames.Application.Xml, typeof(ContractModel)), Is.True);
                Assert.That(formatter.CanRead("Application/XML", typeof(PlainModel)), Is.True);
                Assert.That(formatter.CanRead(MediaTypeNames.Application.Json, typeof(PlainModel)), Is.False);
                Assert.That(formatter.CanWrite("Application/XML", typeof(ContractModel)), Is.True);
                Assert.That(formatter.CanWrite(MediaTypeNames.Application.Json, typeof(PlainModel)), Is.False);
            }
        }

        [TestCase("utf-8")]
        [TestCase("utf-16BE")]
        public async Task ReadAsync_WithXmlSerializer_ReadsObject(string encodingName)
        {
            var encoding = Encoding.GetEncoding(encodingName);
            var expected = new PlainModel { Name = "Test" };
            using var content = new StringContent(expected.ToXmlSerializerString(encoding), encoding, MediaTypeNames.Application.Xml);
            var formatter = new XmlFormatter { Serializer = XmlSerializerKind.XmlSerializer };

            var result = await formatter.ReadAsync(content, typeof(PlainModel));

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("utf-8")]
        [TestCase("utf-16BE")]
        public async Task ReadAsync_WithDataContractSerializer_ReadsObject(string encodingName)
        {
            var encoding = Encoding.GetEncoding(encodingName);
            var expected = new ContractModel { Name = "Test" };
            using var content = new StringContent(expected.ToDataContractString(encoding), encoding, MediaTypeNames.Application.Xml);
            var formatter = new XmlFormatter { Serializer = XmlSerializerKind.DataContractSerializer };

            var result = await formatter.ReadAsync(content, typeof(ContractModel));

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadAsync_WithAuto_UsesDataContractSerializerForDataContractTypes()
        {
            var formatter = new XmlFormatter();

            var contract = await Read(formatter, new ContractModel { Name = "Test" }.ToDataContractString(Encoding.UTF8), typeof(ContractModel));
            var dual = await Read(formatter, new DualModel { Name = "Test" }.ToDataContractString(Encoding.UTF8), typeof(DualModel));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(contract, Is.EqualTo(new ContractModel { Name = "Test" }));
                Assert.That(dual, Is.EqualTo(new DualModel { Name = "Test" }));
            }
        }

        [Test]
        public async Task ReadAsync_WithAuto_UsesXmlSerializerForOtherTypes()
        {
            var formatter = new XmlFormatter();

            var result = await Read(formatter, new PlainModel { Name = "Test" }.ToXmlSerializerString(Encoding.UTF8), typeof(PlainModel));

            Assert.That(result, Is.EqualTo(new PlainModel { Name = "Test" }));
        }

        [Test]
        public async Task ReadAsync_WithFixedSerializer_IgnoresTheAttributes()
        {
            var xmlSerializerFormatter = new XmlFormatter { Serializer = XmlSerializerKind.XmlSerializer };
            var dataContractFormatter = new XmlFormatter { Serializer = XmlSerializerKind.DataContractSerializer };

            var contract = await Read(xmlSerializerFormatter, new ContractModel { Name = "Test" }.ToXmlSerializerString(Encoding.UTF8), typeof(ContractModel));
            var plain = await Read(dataContractFormatter, new PlainModel { Name = "Test" }.ToDataContractString(Encoding.UTF8), typeof(PlainModel));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(contract, Is.EqualTo(new ContractModel { Name = "Test" }));
                Assert.That(plain, Is.EqualTo(new PlainModel { Name = "Test" }));
            }
        }

        [Test]
        public async Task Write_WithAuto_ChoosesSerializerByPayloadType()
        {
            var formatter = new XmlFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Write(formatter, new PlainModel { Name = "Test" }), Is.EqualTo(new PlainModel { Name = "Test" }.ToXmlSerializerString(Encoding.UTF8)));
                Assert.That(await Write(formatter, new ContractModel { Name = "Test" }), Is.EqualTo(new ContractModel { Name = "Test" }.ToDataContractString(Encoding.UTF8)));
                Assert.That(await Write(formatter, new DualModel { Name = "Test" }), Is.EqualTo(new DualModel { Name = "Test" }.ToDataContractString(Encoding.UTF8)));
            }
        }

        [Test]
        public async Task Write_WithFixedSerializer_IgnoresTheAttributes()
        {
            var xmlSerializerFormatter = new XmlFormatter { Serializer = XmlSerializerKind.XmlSerializer };
            var dataContractFormatter = new XmlFormatter { Serializer = XmlSerializerKind.DataContractSerializer };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Write(xmlSerializerFormatter, new ContractModel { Name = "Test" }), Is.EqualTo(new ContractModel { Name = "Test" }.ToXmlSerializerString(Encoding.UTF8)));
                Assert.That(await Write(dataContractFormatter, new PlainModel { Name = "Test" }), Is.EqualTo(new PlainModel { Name = "Test" }.ToDataContractString(Encoding.UTF8)));
            }
        }

        [Test]
        public async Task DataContractSettings_ApplyInBothDirections()
        {
            var dictionary = new XmlDictionary();
            var formatter = new XmlFormatter
            {
                DataContractSettings = new DataContractSerializerSettings
                {
                    RootName = dictionary.Add("Renamed"),
                    RootNamespace = dictionary.Add("urn:test"),
                },
            };
            var model = new ContractModel { Name = "Test" };

            var written = await Write(formatter, model);
            var read = await Read(formatter, written, typeof(ContractModel));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(written, Does.Contain("<Renamed ").And.Contain("xmlns=\"urn:test\""));
                Assert.That(read, Is.EqualTo(model));
                Assert.ThrowsAsync<SerializationException>(() => Read(new XmlFormatter(), written, typeof(ContractModel)));
            }
        }

        [Test]
        public void Write_WithUnsupportedMediaType_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => new XmlFormatter().Write(new PlainModel(), MediaTypeNames.Application.Json));
        }

        private static async Task<object?> Read(XmlFormatter formatter, string xml, Type modelType)
        {
            using var content = new StringContent(xml, Encoding.UTF8, MediaTypeNames.Application.Xml);
            return await formatter.ReadAsync(content, modelType);
        }

        private static async Task<string> Write(XmlFormatter formatter, object payload)
        {
            using var content = formatter.Write(payload, MediaTypeNames.Application.Xml);
            return await content.ReadAsStringAsync();
        }
    }
}
