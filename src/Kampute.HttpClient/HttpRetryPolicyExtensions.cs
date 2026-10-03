// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.Retry;
    using System;

    /// <summary>
    /// Provides extension methods that use an <see cref="IRetryStrategy"/> for retrying HTTP requests.
    /// </summary>
    public static class HttpRetryPolicyExtensions
    {
        /// <summary>
        /// Creates an HTTP retry policy that retries failed requests as the strategy decides.
        /// </summary>
        /// <param name="source">The retry strategy of the policy.</param>
        /// <returns>A new <see cref="HttpRetryPolicy"/> for <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static HttpRetryPolicy ToHttpRetryPolicy(this IRetryStrategy source) => new(source);
    }
}
