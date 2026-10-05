// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Linq;
    using System.Net.Http.Headers;

    /// <summary>
    /// Provides extension methods that read the retry time a response suggests from its headers.
    /// </summary>
    public static class HttpResponseHeadersExtensions
    {
        /// <summary>
        /// Reads the retry time from the <c>Retry-After</c> header.
        /// </summary>
        /// <param name="headers">The response headers.</param>
        /// <param name="retryAfterTime">
        /// When this method returns, the time the header suggests, or <see langword="null"/> if it has none. A number of seconds is converted to a time
        /// from now.
        /// </param>
        /// <returns><see langword="true"/> if the header has a valid date or number of seconds; otherwise, <see langword="false"/>.</returns>
        public static bool TryExtractRetryAfterTime(this HttpResponseHeaders headers, out DateTimeOffset? retryAfterTime)
        {
            if (headers.RetryAfter is RetryConditionHeaderValue retryAfterHeader)
            {
                if (retryAfterHeader.Date is DateTimeOffset date)
                {
                    retryAfterTime = date;
                    return true;
                }
                if (retryAfterHeader.Delta is TimeSpan delta)
                {
                    retryAfterTime = DateTimeOffset.UtcNow.Add(delta);
                    return true;
                }
            }

            retryAfterTime = default;
            return false;
        }

        /// <summary>
        /// Reads the time when a rate limit resets from the <c>Retry-After</c> header or a rate limit reset header.
        /// </summary>
        /// <param name="headers">The response headers.</param>
        /// <param name="resetTime">When this method returns, the time the headers suggest, or <see langword="null"/> if they suggest none.</param>
        /// <returns><see langword="true"/> if a header has a valid value; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>
        /// The method first looks for a <c>Retry-After</c> header. If there is none, it reads the first rate limit reset header it finds among
        /// <c>ratelimit-reset</c>, <c>rate-limit-reset</c>, <c>x-ratelimit-reset</c> and <c>x-rate-limit-reset</c>.
        /// </para>
        /// <para>
        /// A rate limit reset value from 0 to 86400 is read as a number of seconds from now. A larger value, up to 253402300799 (the Unix time
        /// of <see cref="DateTimeOffset.MaxValue"/>), is read as a Unix time in seconds. Any other value, such as a negative number or a time
        /// in milliseconds, is treated as if the header were missing.
        /// </para>
        /// </remarks>
        public static bool TryExtractRateLimitResetTime(this HttpResponseHeaders headers, out DateTimeOffset? resetTime)
        {
            if (headers.TryExtractRetryAfterTime(out resetTime))
                return true;

            foreach (var name in Constants.RateLimitResetHeaderNames)
            {
                if (headers.TryGetValues(name, out var values))
                {
                    if (long.TryParse(values.FirstOrDefault(), out var value) && value >= 0 && value <= Constants.MaxUnixTimeSeconds)
                    {
                        resetTime = value > Constants.SecondsPerDay
                           ? DateTimeOffset.FromUnixTimeSeconds(value)
                           : DateTimeOffset.UtcNow.AddSeconds(value);
                        return true;
                    }
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Contains constants used throughout this extension class.
        /// </summary>
        private static class Constants
        {
            /// <summary>
            /// The largest rate limit reset value read as seconds from now. Larger values are read as a Unix time in seconds.
            /// </summary>
            public const long SecondsPerDay = 86400;

            /// <summary>
            /// The Unix time in seconds of <see cref="DateTimeOffset.MaxValue"/>, which is the largest rate limit reset value accepted.
            /// </summary>
            public const long MaxUnixTimeSeconds = 253402300799;

            /// <summary>
            /// The collection of possible HTTP header names for a rate limit reset value.
            /// </summary>
            public static readonly string[] RateLimitResetHeaderNames =
            [
                "ratelimit-reset",
                "rate-limit-reset",
                "x-ratelimit-reset",
                "x-rate-limit-reset",
            ];
        }
    }
}
