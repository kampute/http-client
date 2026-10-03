namespace Kampute.HttpClient.Test.Content
{
    using Kampute.HttpClient.Content;
    using Kampute.HttpClient.TestSupport;
    using NUnit.Framework;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class NonOwningContentTests
    {
        [Test]
        public async Task NonOwningContent_SendsOriginalHeadersAndBody()
        {
            var text = "Original content";

            using var originalContent = new StringContent(text, Encoding.UTF8, MediaTypeNames.Text.Plain);
            using var nonOwningContent = new NonOwningContent(originalContent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nonOwningContent.Headers.ContentType, Is.EqualTo(originalContent.Headers.ContentType));
                Assert.That(nonOwningContent.Headers.ContentLength, Is.EqualTo(originalContent.Headers.ContentLength));
                Assert.That(await nonOwningContent.ReadAsStringAsync(), Is.EqualTo(text));
            }
        }

        [Test]
        public async Task Dispose_LeavesOriginalContentUsable()
        {
            var text = "Original content";

            using var originalContent = new StringContent(text);
            new NonOwningContent(originalContent).Dispose();

            Assert.That(await originalContent.ReadAsStringAsync(), Is.EqualTo(text));
        }

        [Test]
        public void IsReusable_ReflectsOriginalContent()
        {
            using var reusableContent = new NonOwningContent(new StringContent("Original content"));
            using var nonReusableContent = new NonOwningContent(new StreamContent(new TestStream(seekable: false)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reusableContent.IsReusable(), Is.True);
                Assert.That(nonReusableContent.IsReusable(), Is.False);
            }
        }
    }
}
