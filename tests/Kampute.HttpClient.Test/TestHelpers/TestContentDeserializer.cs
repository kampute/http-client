namespace Kampute.HttpClient.Test.TestHelpers
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    internal class TestContentDeserializer : IHttpContentDeserializer
    {
        public IReadOnlyCollection<string> SupportedMediaTypes { get; } = [Constants.TestMediaType];

        public IEnumerable<string> GetSupportedMediaTypes(Type? modelType)
        {
            return modelType is not null && CanParse(modelType) ? SupportedMediaTypes : [];
        }

        public bool CanDeserialize(string mediaType, Type? modelType)
        {
            return SupportedMediaTypes.Contains(mediaType) && modelType is not null && CanParse(modelType);
        }

        public async Task<object?> DeserializeAsync(HttpContent content, Type modelType, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(modelType);

            var str = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (typeof(TestErrorResponse) == modelType) ? new TestErrorResponse(str) : Convert.ChangeType(str, modelType);
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
