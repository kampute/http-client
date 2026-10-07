namespace Kampute.HttpClient.Test.Content.Compression
{
    using Kampute.HttpClient.Content.Compression;
    using NUnit.Framework;
    using System;
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(compressedContent.Headers.ContentType, Is.EqualTo(originalContent.Headers.ContentType));
                Assert.That(compressedContent.Headers.ContentEncoding, Contains.Item("gzip"));
            }

            var compressedStream = await compressedContent.ReadAsStreamAsync();

            Assert.That(compressedStream.Length, Is.LessThan(originalContent.Headers.ContentLength.GetValueOrDefault()));

            using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(decompressionStream, Encoding.UTF32);

            Assert.That(reader.ReadToEnd(), Is.EqualTo(text));
        }

        [Test]
        public async Task Constructor_WithOriginalLengthAndHash_DoesNotCopyThem()
        {
            using var originalContent = new StringContent("Original content");
            originalContent.Headers.ContentMD5 = [1, 2, 3, 4];
            _ = originalContent.Headers.ContentLength;

            using var compressedContent = new GzipCompressedContent(originalContent, CompressionLevel.Optimal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(compressedContent.Headers.ContentLength, Is.Null);
                Assert.That(compressedContent.Headers.ContentMD5, Is.Null);
            }

            // HttpClient on .NET Framework buffers content of unknown length and then sends the length of the buffer.
            await compressedContent.LoadIntoBufferAsync();
            var compressedBytes = await compressedContent.ReadAsByteArrayAsync();

            Assert.That(compressedContent.Headers.ContentLength, Is.EqualTo(compressedBytes.Length));
        }

        [Test]
        public async Task Dispose_DisposesOriginalContent()
        {
            using var originalContent = new StringContent("Original content");

            new GzipCompressedContent(originalContent, CompressionLevel.Optimal).Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => originalContent.ReadAsStringAsync());
        }
    }
}
