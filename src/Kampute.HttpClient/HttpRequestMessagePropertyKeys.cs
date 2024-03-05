// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System.Net.Http;

    /// <summary>
    /// Defines constant keys for storing and identifying custom properties in an <see cref="HttpRequestMessage"/>.
    /// </summary>
    public static class HttpRequestMessagePropertyKeys
    {
        /// <summary>
        /// A key used to store and identify the clone generation property in an <see cref="HttpRequestMessage"/>.
        /// This property tracks how many times the request has been cloned.
        /// </summary>
        public const string CloneGeneration = nameof(HttpRestClient) + "." + nameof(CloneGeneration);

        /// <summary>
        /// A key used to store and identify the transaction identifier property in an <see cref="HttpRequestMessage"/>.
        /// This property identifies the request and its clones.
        /// </summary>
        public const string TransactionId = nameof(HttpRestClient) + "." + nameof(TransactionId);

        /// <summary>
        /// A key used to store and identify the retry scheduler property in an <see cref="HttpRequestMessage"/>.
        /// This property holds the active scheduler associated with the request, if any, managing retry logic for transient failures.
        /// </summary>
        public const string RetryScheduler = nameof(HttpRestClient) + "." + nameof(RetryScheduler);
    }
}
