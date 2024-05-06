using Kampute.HttpClient.Utilities;

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http.Headers;

    /// <summary>
    /// Provides a cache for <see cref="MediaTypeWithQualityHeaderValue"/> instances to improve performance by reusing instances for
    /// frequently requested media types and quality settings.
    /// </summary>
    public static class MediaTypeHeaderValueStore
    {
        /// <summary>
        /// Retrieves a <see cref="MediaTypeWithQualityHeaderValue"/> from the cache or creates a new one if it does not exist.
        /// </summary>
        /// <param name="mediaType">The media type as a string.</param>
        /// <returns>A <see cref="MediaTypeWithQualityHeaderValue"/> corresponding to the specified media type.</returns>
        public static MediaTypeWithQualityHeaderValue Get(string mediaType) => WithoutQuality.Store.Get(mediaType);

        /// <summary>
        /// Retrieves a <see cref="MediaTypeWithQualityHeaderValue"/> from the cache or creates a new one if it does not exist.
        /// </summary>
        /// <param name="mediaType">The media type as a string.</param>
        /// <param name="quality">The quality factor associated with this media type, expressed as a value between 0 and 1.</param>
        /// <returns>A <see cref="MediaTypeWithQualityHeaderValue"/> corresponding to the specified media type and quality factor.</returns>
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
