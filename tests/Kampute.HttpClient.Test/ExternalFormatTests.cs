namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.Content.Abstracts;
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Shows that a format the library does not ship, here a binary encoding of a point, can be added using only public API, in the three styles
    /// that consumers use.
    /// </summary>
    [TestFixture]
    public class ExternalFormatTests
    {
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _client;

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, disposeHandler: false);
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
        public async Task ContentClassWithOwnHelper_SendsPayload()
        {
            _client.ContentFormatters.Add(new TestContentFormatter());
            var received = default(Point?);
            _mockMessageHandler.MockHttpResponse(request =>
            {
                received = PointEncoding.Decode(request.Content!.ReadAsByteArrayAsync().Result);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new TestContent("accepted") };
            });

            var result = await _client.PostAsPointContentAsync<string>("/points", new Point(3, -4));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(received, Is.EqualTo(new Point(3, -4)));
                Assert.That(result, Is.EqualTo("accepted"));
            }
        }

        [Test]
        public async Task ReceiveOnlyFormatter_ReadsResponseAndFeedsAcceptHeader()
        {
            _client.ContentFormatters.Add(new PointReader());
            var acceptedMediaTypes = default(string);
            _mockMessageHandler.MockHttpResponse(request =>
            {
                acceptedMediaTypes = request.Headers.Accept.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new PointContent(new Point(7, 8)) };
            });

            var result = await _client.GetAsync<Point>("/points/1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(new Point(7, 8)));
                Assert.That(acceptedMediaTypes, Is.EqualTo(PointEncoding.MediaType));
            }
        }

        [Test]
        public async Task TwoWayFormatterWithOneLineHelpers_SendsAndReadsPayload()
        {
            _client.UsePoints();
            _mockMessageHandler.MockHttpResponse(request =>
            {
                var point = PointEncoding.Decode(request.Content!.ReadAsByteArrayAsync().Result);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new PointContent(new Point(point.Y, point.X)) };
            });

            var result = await _client.PostAsPointAsync<Point>("/points/swap", new Point(1, 2));

            Assert.That(result, Is.EqualTo(new Point(2, 1)));
        }

        [Test]
        public async Task TwoWayFormatterWithOneLineHelpers_WithoutRegistration_SendsPayload()
        {
            var received = default(Point?);
            _mockMessageHandler.MockHttpResponse(request =>
            {
                received = PointEncoding.Decode(request.Content!.ReadAsByteArrayAsync().Result);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            await _client.PostAsPointAsync("/points", new Point(5, 6));

            Assert.That(received, Is.EqualTo(new Point(5, 6)));
        }
    }

    public readonly record struct Point(int X, int Y);

    internal static class PointEncoding
    {
        public const string MediaType = "application/x-point";

        public static byte[] Encode(Point point)
        {
            var bytes = new byte[8];
            BitConverter.GetBytes(point.X).CopyTo(bytes, 0);
            BitConverter.GetBytes(point.Y).CopyTo(bytes, 4);
            return bytes;
        }

        public static Point Decode(byte[] bytes) => new(BitConverter.ToInt32(bytes, 0), BitConverter.ToInt32(bytes, 4));
    }

    // Style 1: a content class and a helper built on PostAsync(uri, HttpContent), as custom request formats are written before 3.0.

    public sealed class PointContent : HttpContent
    {
        private readonly byte[] _bytes;

        public PointContent(Point point)
        {
            _bytes = PointEncoding.Encode(point);
            Headers.ContentType = new MediaTypeHeaderValue(PointEncoding.MediaType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(_bytes, 0, _bytes.Length);

        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return true;
        }
    }

    public static class PointContentExtensions
    {
        public static Task<T?> PostAsPointContentAsync<T>(this HttpRestClient client, string uri, Point point, CancellationToken cancellationToken = default)
        {
            return client.PostAsync<T>(uri, new PointContent(point), cancellationToken);
        }
    }

    // Style 2: a receive-only formatter, the migration of a custom response deserializer.

    public sealed class PointReader : HttpContentFormatter
    {
        public PointReader()
            : base([PointEncoding.MediaType], [])
        {
        }

        protected override bool CanReadType(Type modelType) => modelType == typeof(Point);

        protected override async Task<object?> ReadContentAsync(HttpContent content, Type modelType, CancellationToken cancellationToken)
        {
            return PointEncoding.Decode(await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
        }
    }

    // Style 3: a two-way formatter with one-line helpers built on SendObjectAsync.

    public sealed class PointFormatter : HttpContentFormatter
    {
        public PointFormatter()
            : base([PointEncoding.MediaType], [PointEncoding.MediaType])
        {
        }

        protected override bool CanReadType(Type modelType) => modelType == typeof(Point);

        protected override bool CanWriteType(Type payloadType) => payloadType == typeof(Point);

        protected override async Task<object?> ReadContentAsync(HttpContent content, Type modelType, CancellationToken cancellationToken)
        {
            return PointEncoding.Decode(await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
        }

        protected override HttpContent CreateContent(object payload, string mediaType) => new PointContent((Point)payload);
    }

    public static class PointFormatterExtensions
    {
        public static PointFormatter UsePoints(this HttpRestClient client)
        {
            var formatter = client.ContentFormatters.Find<PointFormatter>();
            if (formatter is null)
            {
                formatter = new PointFormatter();
                client.ContentFormatters.Add(formatter);
            }
            return formatter;
        }

        public static Task<T?> PostAsPointAsync<T>(this HttpRestClient client, string uri, Point point, CancellationToken cancellationToken = default)
            => client.SendObjectAsync<T>(HttpVerb.Post, uri, point, client.ContentFormatters.FindOrDefault<PointFormatter>(), cancellationToken);

        public static Task PostAsPointAsync(this HttpRestClient client, string uri, Point point, CancellationToken cancellationToken = default)
            => client.SendObjectAsync(HttpVerb.Post, uri, point, client.ContentFormatters.FindOrDefault<PointFormatter>(), cancellationToken);
    }
}
