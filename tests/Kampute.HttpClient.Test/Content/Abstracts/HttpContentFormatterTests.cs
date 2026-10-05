namespace Kampute.HttpClient.Test.Content.Abstracts
{
    using Kampute.HttpClient.Content.Abstracts;
    using NUnit.Framework;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpContentFormatterTests
    {
        [Test]
        public void MediaTypeQueries_HonorTheTypeFiltersAndIgnoreCase()
        {
            var formatter = new StringOnlyFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formatter.GetReadableMediaTypes(typeof(string)), Is.EqualTo(new[] { "text/x-read" }));
                Assert.That(formatter.GetReadableMediaTypes(typeof(int)), Is.Empty);
                Assert.That(formatter.GetWritableMediaTypes(typeof(string)), Is.EqualTo(new[] { "text/x-write" }));
                Assert.That(formatter.GetWritableMediaTypes(typeof(int)), Is.Empty);
                Assert.That(formatter.CanRead("TEXT/X-READ", typeof(string)), Is.True);
                Assert.That(formatter.CanRead("text/x-write", typeof(string)), Is.False);
                Assert.That(formatter.CanWrite("TEXT/X-WRITE", typeof(string)), Is.True);
                Assert.That(formatter.CanWrite("text/x-read", typeof(string)), Is.False);
                Assert.That(formatter.CanWrite("text/x-write", typeof(int)), Is.False);
            }
        }

        [Test]
        public void Write_WithUnsupportedMediaTypeOrPayload_ThrowsNotSupportedException()
        {
            var formatter = new StringOnlyFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<NotSupportedException>(() => formatter.Write("payload", "text/x-read"));
                Assert.Throws<NotSupportedException>(() => formatter.Write(42, "text/x-write"));
            }
        }

        [Test]
        public void Write_WithNullArgument_ThrowsArgumentNullException()
        {
            var formatter = new StringOnlyFormatter();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentNullException>(() => formatter.Write(null!, "text/x-write"));
                Assert.Throws<ArgumentNullException>(() => formatter.Write("payload", null!));
            }
        }

        [Test]
        public void ReadAsync_WithNullArgument_ThrowsBeforeReturningTask()
        {
            var formatter = new StringOnlyFormatter();
            using var content = new StringContent("text");

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentNullException>(() => formatter.ReadAsync(null!, typeof(string)));
                Assert.Throws<ArgumentNullException>(() => formatter.ReadAsync(content, null!));
            }
        }

        [Test]
        public async Task ReadAsyncAndWrite_DelegateToTheOverrides()
        {
            var formatter = new StringOnlyFormatter();

            using var content = formatter.Write("payload", "text/x-write");
            var result = await formatter.ReadAsync(content, typeof(string));

            Assert.That(result, Is.EqualTo("payload"));
        }

        [Test]
        public void ReadAsyncAndWrite_WithoutOverrides_ThrowNotSupportedException()
        {
            var formatter = new NoOverridesFormatter();
            using var content = new StringContent("text");

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<NotSupportedException>(() => formatter.ReadAsync(content, typeof(string)));
                Assert.Throws<NotSupportedException>(() => formatter.Write("payload", "text/x-write"));
            }
        }

        private sealed class StringOnlyFormatter : HttpContentFormatter
        {
            public StringOnlyFormatter()
                : base(["text/x-read"], ["text/x-write"])
            {
            }

            protected override bool CanReadType(Type modelType) => modelType == typeof(string);

            protected override bool CanWriteType(Type payloadType) => payloadType == typeof(string);

            protected override async Task<object?> ReadContentAsync(HttpContent content, Type modelType, CancellationToken cancellationToken)
            {
                return await content.ReadAsStringAsync(cancellationToken);
            }

            protected override HttpContent CreateContent(object payload, string mediaType)
            {
                return new StringContent((string)payload);
            }
        }

        private sealed class NoOverridesFormatter : HttpContentFormatter
        {
            public NoOverridesFormatter()
                : base(["text/x-read"], ["text/x-write"])
            {
            }
        }
    }
}
