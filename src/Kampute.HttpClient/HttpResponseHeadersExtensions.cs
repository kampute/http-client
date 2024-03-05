// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Linq;
    using System.Net.Http.Headers;

    /// <summary>
    /// Provides extension methods for <see cref="HttpResponseHeaders"/> to facilitate HTTP response processing.
    /// </summary>
    public static class HttpResponseHeadersExtensions
    {
        /// <summary>
        /// Attempts to extract the retry-after time from the HTTP response headers.
        /// </summary>
        /// <param name="headers">The HTTP response headers.</param>
        /// <param name="retryAfterTime">When this method returns, contains the extracted time if the operation is successful; otherwise, <c>null</c>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the time could be successfully extracted and parsed; otherwise, <c>false</c>.</returns>
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
        /// Attempts to extract the rate limit reset time from the HTTP response headers.
        /// </summary>
        /// <param name="headers">The HTTP response headers.</param>
        /// <param name="resetTime">When this method returns, contains the extracted time if the operation is successful; otherwise, <c>null</c>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the time could be successfully extracted and parsed; otherwise, <c>false</c>.</returns>
        public static bool TryExtractRateLimitResetTime(this HttpResponseHeaders headers, out DateTimeOffset? resetTime)
        {
            if (headers.TryExtractRetryAfterTime(out resetTime))
                return true;

            foreach (var name in Constants.RateLimitResetHeaderNames)
            {
                if (headers.TryGetValues(name, out var values))
                {
                    if (long.TryParse(values.FirstOrDefault(), out var value))
                    {
                        resetTime = value > 86400 // seconds per day
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
