namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class StreamingResponseTests
    {
        private const int BufferLimit = 16;

        private static readonly byte[] LargeBody = [.. Enumerable.Range(0, 1024).Select(i => (byte)i)];

        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _client;

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, false)
            {
                MaxResponseContentBufferSize = BufferLimit,
            };
            _client = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
        }

        [TearDown]
        public void Cleanup()
        {
            _client.Dispose();
        }

        [Test]
        public async Task GetAsStreamAsync_WithBodyLargerThanBufferLimit_StreamsBody()
        {
            _mockMessageHandler.MockHttpResponse(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(LargeBody) });

            using var bodyStream = await _client.GetAsStreamAsync("/resource");
            using var resultStream = new MemoryStream();
            await bodyStream.CopyToAsync(resultStream);

            Assert.That(resultStream.ToArray(), Is.EqualTo(LargeBody));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task GetAsStreamAsync_WhenOpeningBodyFails_DisposesResponseContent(bool failSynchronously)
        {
            var failure = new IOException("Body failed.");
            using var content = new FailingContent(failure, failSynchronously);
            _mockMessageHandler.MockHttpResponse(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetAsStreamAsync("/resource"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(content.IsDisposed, Is.True);
                Assert.That(exception.InnerException, Is.SameAs(failure));
            }
        }

        [Test]
        public async Task GetToStreamAsync_WithBodyLargerThanBufferLimit_StreamsBody()
        {
            _mockMessageHandler.MockHttpResponse(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(LargeBody) });

            using var resultStream = new MemoryStream();
            await _client.GetToStreamAsync("/resource", resultStream);

            Assert.That(resultStream.ToArray(), Is.EqualTo(LargeBody));
        }

        [Test]
        public async Task DownloadAsync_WithBodyLargerThanBufferLimit_StreamsBody()
        {
            _mockMessageHandler.MockHttpResponse(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(LargeBody) });

            using var resultStream = await _client.DownloadAsync(HttpMethod.Get, "/resource", null, _ => new MemoryStream());

            Assert.That(((MemoryStream)resultStream).ToArray(), Is.EqualTo(LargeBody));
        }

        [Test]
        public void GetToStreamAsync_WhenCanceledDuringCopy_StopsCopying()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            _mockMessageHandler.MockHttpResponse(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new EndlessStream()) });

            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));
            using var resultStream = new MemoryStream();

            Assert.That(() => _client.GetToStreamAsync("/resource", resultStream, cancellationTokenSource.Token), Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public async Task DownloadAsync_WhenCopyFails_DisposesDestinationStream()
        {
            _mockMessageHandler.MockHttpResponse(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3, 4]) });

            var destination = new FailingStream();

            var exception = await Assert.CatchAsync(() => _client.DownloadAsync(HttpMethod.Get, "/resource", null, _ => destination));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(destination.IsDisposed, Is.True);
                Assert.That(exception, Is.InstanceOf<IOException>());
            }
        }

        /// <summary>
        /// A stream that never ends; each read waits until it is canceled.
        /// </summary>
        private sealed class EndlessStream : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return 0;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return 0;
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        /// <summary>
        /// A writable stream whose writes always fail, and which records whether it was disposed.
        /// </summary>
        private sealed class FailingStream : MemoryStream
        {
            public bool IsDisposed { get; private set; }

            public override void Write(byte[] buffer, int offset, int count) => throw new IOException("Write failed.");
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new IOException("Write failed.");
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("Write failed.");

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        private sealed class FailingContent : HttpContent
        {
            private readonly IOException _failure;
            private readonly bool _failSynchronously;

            public FailingContent(IOException failure, bool failSynchronously)
            {
                _failure = failure;
                _failSynchronously = failSynchronously;
            }

            public bool IsDisposed { get; private set; }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                if (_failSynchronously)
                    throw _failure;

                return Task.FromException(_failure);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
