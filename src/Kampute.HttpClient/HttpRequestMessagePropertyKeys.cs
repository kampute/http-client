// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Net.Http;

    /// <summary>
    /// Defines constant keys for storing and identifying custom properties in an <see cref="HttpRequestMessage"/>.
    /// </summary>
    public static class HttpRequestMessagePropertyKeys
    {
        /// <summary>
        /// A key used to store and identify the property within an <see cref="HttpRequestMessage"/> that tracks
        /// how many times the request has been cloned. 
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="int"/>.
        /// </remarks>
        public const string CloneGeneration = nameof(HttpRestClient) + "." + nameof(CloneGeneration);

        /// <summary>
        /// A key used to store and identify the property within an <see cref="HttpRequestMessage"/> that identifies
        /// the request and its clones.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="Guid"/>.
        /// </remarks>
        public const string TransactionId = nameof(HttpRestClient) + "." + nameof(TransactionId);

        /// <summary>
        /// A key used to store and identify the property within an <see cref="HttpRequestMessage"/> that identifies
        /// the type of expected .NET object in the response.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="Type"/>.
        /// </remarks>
        public const string ResponseObjectType = nameof(HttpRestClient) + "." + nameof(ResponseObjectType);

        /// <summary>
        /// A key used to store and identify the property within an <see cref="HttpRequestMessage"/> that references
        /// the <see cref="IRetryScheduler"/> instances associated with the request which are responsible for scheduling
        /// the retry logic for transient failures.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value of this property is of type <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> with <see cref="object"/> keys and
        /// <see cref="IRetryScheduler"/> values. Each key is the source that handles one kind of failure: the <see cref="HttpRestClient"/>
        /// for connection failures, or the <see cref="IHttpErrorHandler"/> for error responses. A <see langword="null"/> value means that
        /// the source decided not to retry.
        /// </para>
        /// <para>
        /// Because each kind of failure has its own retry budget, a request that fails in several ways can be retried more times in total
        /// than any single budget allows.
        /// </para>
        /// </remarks>
        /// <seealso cref="HttpRequestErrorContext.ScheduleRetryAsync(object, Func{HttpRequestErrorContext, IRetryScheduler}, System.Threading.CancellationToken)"/>
        public const string RetryScheduler = nameof(HttpRestClient) + "." + nameof(RetryScheduler);

        /// <summary>
        /// A key used to store and identify the property within an <see cref="HttpRequestMessage"/> that references
        /// the <see cref="IHttpErrorHandler"/> instance associated with the request, which is responsible for processing
        /// and potentially recovering from errors in the response.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="IHttpErrorHandler"/>.
        /// </remarks>
        public const string ErrorHandler = nameof(HttpRestClient) + "." + nameof(ErrorHandler);

        /// <summary>
        /// A key used to store and identify the property within an <see cref="HttpRequestMessage"/> that indicates
        /// '401 Unauthorized' errors should not be automatically handled.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="bool"/>.
        /// </remarks>
        public const string SkipUnauthorizedHandling = nameof(HttpRestClient) + "." + nameof(SkipUnauthorizedHandling);
    }
}
