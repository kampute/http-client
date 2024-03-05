namespace Kampute.HttpClient.NewtonsoftJson.Test
{
    using NUnit.Framework;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class JsonContentTests
    {
        [Test]
        public async Task SetsContentCorrectly()
        {
            var model = new TestModel { Name = "Test" };
            var expectedString = model.ToJsonString();

            using var jsonContent = new JsonContent(model) { Settings = TestModel.JsonSettings };

            var jsonString = await jsonContent.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(jsonString, Is.EqualTo(expectedString));
                Assert.That(jsonContent.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                Assert.That(jsonContent.Headers.ContentType?.CharSet, Is.EqualTo(Encoding.UTF8.WebName));
            });
        }
    }
}
