namespace Kampute.HttpClient.Json.Test
{
    using NUnit.Framework;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class JsonFormatterTests
    {
        [Test]
        public void MediaTypes_AreApplicationJsonInBothDirections()
        {
            var formatter = new JsonFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formatter.GetReadableMediaTypes(typeof(TestModel)), Is.EqualTo(new[] { MediaTypeNames.Application.Json }));
                Assert.That(formatter.GetWritableMediaTypes(typeof(TestModel)), Is.EqualTo(new[] { MediaTypeNames.Application.Json }));
            }
        }

        [Test]
        public void CanReadAndCanWrite_ForSupportedMediaType_ReturnTrue()
        {
            var formatter = new JsonFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formatter.CanRead(MediaTypeNames.Application.Json, typeof(TestModel)), Is.True);
                Assert.That(formatter.CanWrite(MediaTypeNames.Application.Json, typeof(TestModel)), Is.True);
            }
        }

        [Test]
        public void CanReadAndCanWrite_ForSupportedMediaTypeInDifferentCase_ReturnTrue()
        {
            var formatter = new JsonFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formatter.CanRead("Application/JSON", typeof(TestModel)), Is.True);
                Assert.That(formatter.CanWrite("Application/JSON", typeof(TestModel)), Is.True);
            }
        }

        [Test]
        public void CanReadAndCanWrite_ForUnsupportedMediaType_ReturnFalse()
        {
            var formatter = new JsonFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formatter.CanRead(MediaTypeNames.Application.Xml, typeof(TestModel)), Is.False);
                Assert.That(formatter.CanWrite(MediaTypeNames.Application.Xml, typeof(TestModel)), Is.False);
            }
        }

        [Test]
        public async Task ReadAsync_WithUtf8EncodedJsonContent_ReturnsCorrectObject()
        {
            var expected = new TestModel { Name = "Test" };
            using var content = new StringContent(expected.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var formatter = new JsonFormatter { Options = TestModel.JsonOption };

            var result = await formatter.ReadAsync(content, typeof(TestModel)) as TestModel;

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadAsync_WithNonUtf8EncodedJsonContent_ReturnsCorrectObject()
        {
            var expected = new TestModel { Name = "Test" };
            using var content = new StringContent(expected.ToJsonString(), Encoding.BigEndianUnicode, MediaTypeNames.Application.Json);
            var formatter = new JsonFormatter { Options = TestModel.JsonOption };

            var result = await formatter.ReadAsync(content, typeof(TestModel)) as TestModel;

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public async Task Write_CreatesJsonContentWithTheFormatterOptions()
        {
            var options = TestModel.JsonOption;
            var formatter = new JsonFormatter { Options = options };
            var model = new TestModel { Name = "Test" };

            using var content = formatter.Write(model, MediaTypeNames.Application.Json);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(content, Is.TypeOf<JsonContent>());
                Assert.That(((JsonContent)content).Options, Is.SameAs(options));
                Assert.That(await content.ReadAsStringAsync(), Is.EqualTo(model.ToJsonString()));
            }
        }
    }
}
