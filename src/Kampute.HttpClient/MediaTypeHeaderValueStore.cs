namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Utilities;
    using System;
    using System.Net.Http.Headers;

    /// <summary>
    /// Provides shared <see cref="MediaTypeWithQualityHeaderValue"/> instances for <c>Accept</c> headers, so that a value is not parsed again for every
    /// request.
    /// </summary>
    public static class MediaTypeHeaderValueStore
    {
        /// <summary>
        /// Returns the shared header value of a media type without a quality factor.
        /// </summary>
        /// <param name="mediaType">The media type, such as <c>application/json</c>.</param>
        /// <returns>The shared <see cref="MediaTypeWithQualityHeaderValue"/> of <paramref name="mediaType"/>.</returns>
        public static MediaTypeWithQualityHeaderValue Get(string mediaType) => WithoutQuality.Store.Get(mediaType);

        /// <summary>
        /// Returns the shared header value of a media type with a quality factor.
        /// </summary>
        /// <param name="mediaType">The media type, such as <c>application/json</c>.</param>
        /// <param name="quality">The quality factor, from 0 to 1.</param>
        /// <returns>The shared <see cref="MediaTypeWithQualityHeaderValue"/> of <paramref name="mediaType"/> and <paramref name="quality"/>.</returns>
        public static MediaTypeWithQualityHeaderValue Get(string mediaType, float quality) => WithQuality.Store.Get((mediaType, quality));

        /// <summary>
        /// Manages the caching of <see cref="MediaTypeWithQualityHeaderValue"/> instances without quality factor.
        /// </summary>
        private static class WithoutQuality
        {
            public static readonly FlyweightCache<string, MediaTypeWithQualityHeaderValue> Store =
                new(mediaType => new MediaTypeWithQualityHeaderValue(mediaType), StringComparer.Ordinal);
        }

        /// <summary>
        /// Manages the caching of <see cref="MediaTypeWithQualityHeaderValue"/> instances with quality factor.
        /// </summary>
        private static class WithQuality
        {
            public static readonly FlyweightCache<(string, float), MediaTypeWithQualityHeaderValue> Store =
                new(h => new MediaTypeWithQualityHeaderValue(h.Item1, h.Item2));
        }
    }
}
