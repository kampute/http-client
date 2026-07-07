namespace Kampute.HttpClient.TestSupport
{
    using Kampute.HttpClient;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Text;

    public static class CompressedContentHelpers
    {
        public static string ReadCompressedContent(HttpContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            using var compressedStream = new MemoryStream();
            content.CopyToAsync(compressedStream).GetAwaiter().GetResult();
            compressedStream.Position = 0;

            using Stream decompressedStream = content.Headers.ContentEncoding.ToString() switch
            {
                "gzip" => new GZipStream(compressedStream, CompressionMode.Decompress),
                "deflate" => new DeflateStream(compressedStream, CompressionMode.Decompress),
                _ => throw new InvalidOperationException("Unsupported encoding")
            };
            using var reader = new StreamReader(decompressedStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public static HttpContent CompressContent(HttpContent content, string encoding)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(encoding);

            return encoding switch
            {
                "gzip" => content.AsGzip(),
                "deflate" => content.AsDeflate(),
                _ => throw new InvalidOperationException("Unsupported encoding")
            };
        }
    }
}
