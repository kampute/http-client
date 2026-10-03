// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryManagement
{
    using Kampute.Retry;
    using System;

    /// <summary>
    /// Provides extension methods that use an <see cref="IRetryStrategy"/> for retrying HTTP requests.
    /// </summary>
    public static class RetryStrategyHttpExtensions
    {
        /// <summary>
        /// Converts an <see cref="IRetryStrategy"/> into a <see cref="BackoffStrategy"/>, creating a factory capable of producing retry sessions based
        /// on the provided strategy.
        /// </summary>
        /// <param name="source">The retry strategy to use for creating the sessions.</param>
        /// <returns>A new instance of <see cref="BackoffStrategy"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static BackoffStrategy ToBackoffStrategy(this IRetryStrategy source) => new(source);
    }
}
