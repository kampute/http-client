namespace Kampute.HttpClient.TestSupport
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class TestContentFormatter : IHttpContentFormatter
    {
        public IReadOnlyCollection<string> SupportedMediaTypes { get; } = [Constants.TestMediaType];

        public IEnumerable<string> GetReadableMediaTypes(Type? modelType)
        {
            return modelType is not null && CanParse(modelType) ? SupportedMediaTypes : [];
        }

        public bool CanRead(string mediaType, Type? modelType)
        {
            return SupportedMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase) && modelType is not null && CanParse(modelType);
        }

        public async Task<object?> ReadAsync(HttpContent content, Type modelType, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(modelType);

            var str = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (typeof(TestErrorResponse) == modelType) ? new TestErrorResponse(str) : Convert.ChangeType(str, modelType);
        }

        public IEnumerable<string> GetWritableMediaTypes(Type payloadType)
        {
            return payloadType is not null && CanParse(payloadType) ? SupportedMediaTypes : [];
        }

        public bool CanWrite(string mediaType, Type payloadType)
        {
            return SupportedMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase) && payloadType is not null && CanParse(payloadType);
        }

        public HttpContent Write(object payload, string mediaType)
        {
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(mediaType);

            return new TestContent(payload);
        }

        private static bool CanParse(Type modelType)
        {
            return modelType.IsPrimitive
                || modelType.IsEnum
                || typeof(string) == modelType
                || typeof(decimal) == modelType
                || typeof(Guid) == modelType
                || typeof(DateTime) == modelType
                || typeof(DateTimeOffset) == modelType
                || typeof(TestErrorResponse) == modelType;
        }
    }
}
