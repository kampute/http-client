namespace Kampute.HttpClient.Test.Content.Compression
{
    using Kampute.HttpClient.Content.Compression;
    using NUnit.Framework;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class GzipCompressedContentTests
    {
        [Test]
        public async Task GzipCompressedContent_CompressesDataCorrectly()
        {
            var text = "This string is encoded in UTF-32 to increase its byte size for effective GZIP compression testing.";

            using var originalContent = new StringContent(text, Encoding.UTF32, MediaTypeNames.Text.Plain);
            using var compressedContent = new GzipCompressedContent(originalContent, CompressionLevel.Optimal);

            Assert.Multiple(() =>
            {
                Assert.That(compressedContent.Headers.ContentType, Is.EqualTo(originalContent.Headers.ContentType));
                Assert.That(compressedContent.Headers.ContentEncoding, Contains.Item("gzip"));
            });

            var compressedStream = await compressedContent.ReadAsStreamAsync();

            Assert.That(compressedStream.Length, Is.LessThan(originalContent.Headers.ContentLength.GetValueOrDefault()));

            using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(decompressionStream, Encoding.UTF32);

            Assert.That(reader.ReadToEnd(), Is.EqualTo(text));
        }
    }
}
