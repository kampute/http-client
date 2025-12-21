namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.Content.Compression;
    using Kampute.HttpClient.Test.TestHelpers;
    using NUnit.Framework;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    [TestFixture]
    public class HttpContentExtensionsTests
    {
        [Test]
        public void FindCharacterEncoding_WithCharSet_ReturnsEncoding()
        {
            using var content = new StringContent("Test content", Encoding.UTF8);

            var encoding = content.FindCharacterEncoding();

            Assert.That(encoding, Is.EqualTo(Encoding.UTF8));
        }

        [Test]
        public void FindCharacterEncoding_WithoutCharSet_ReturnsNull()
        {
            using var content = new ByteArrayContent([1, 2, 3]);

            var encoding = content.FindCharacterEncoding();

            Assert.That(encoding, Is.Null);
        }

        [Test]
        public void FindCharacterEncoding_WithUnsupportedCharSet_ThrowsArgumentException()
        {
            using var content = new StringContent("Test content");
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "unsupported-charset" };

            Assert.That(content.FindCharacterEncoding, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void IsReusable_WhenContentIsReusable_ReturnsTrue()
        {
            using var content = new StreamContent(new TestStream(seekable: true));

            var result = content.IsReusable();

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsReusable_WhenContentIsNotReusable_ReturnsFalse()
        {
            using var content = new StreamContent(new TestStream(seekable: false));

            var result = content.IsReusable();

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsReusable_WhenCompressedContentIsReusable_ReturnsTrue()
        {
            var originalContent = new StreamContent(new TestStream(seekable: true));
            using var content = new DeflateCompressedContent(originalContent);

            var result = content.IsReusable();

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsReusable_WhenCompressedContentIsNotReusable_ReturnsFalse()
        {
            var originalContent = new StreamContent(new TestStream(seekable: false));
            using var content = new DeflateCompressedContent(originalContent);

            var result = content.IsReusable();

            Assert.That(result, Is.False);
        }

        [Test]
        public void AsGzip_ReturnsGzipCompressedContent()
        {
            using var content = new StringContent("Test content");
            using var compressedContent = content.AsGzip();

            Assert.That(compressedContent, Is.InstanceOf<GzipCompressedContent>());
        }

        [Test]
        public void AsDeflate_ReturnsDeflateCompressedContent()
        {
            using var content = new StringContent("Test content");
            using var compressedContent = content.AsDeflate();

            Assert.That(compressedContent, Is.InstanceOf<DeflateCompressedContent>());
        }
    }
}
