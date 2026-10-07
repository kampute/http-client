// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// Provides the keys of the request properties that <see cref="HttpRestClient"/> sets and reads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On .NET 5 and later, read these properties through <c>HttpRequestMessage.Options</c> with an <c>HttpRequestOptionsKey&lt;TValue&gt;</c>
    /// named after the key, because <c>HttpRequestMessage.Properties</c> is obsolete there. Both show the same values. On .NET Framework, read
    /// them through <c>HttpRequestMessage.Properties</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (request.Options.TryGetValue(new HttpRequestOptionsKey&lt;Guid&gt;(HttpRequestMessagePropertyKeys.TransactionId), out var transactionId))
    ///     Console.WriteLine($"Transaction {transactionId}");
    /// </code>
    /// </example>
    public static class HttpRequestMessagePropertyKeys
    {
        /// <summary>
        /// The key of the property that counts how many copies separate a request from the original request.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="int"/>. Read it through <see cref="HttpRequestMessageExtensions.GetCloneGeneration"/>
        /// and <see cref="HttpRequestMessageExtensions.IsCloned"/>.
        /// </remarks>
        internal const string CloneGeneration = nameof(HttpRestClient) + "." + nameof(CloneGeneration);

        /// <summary>
        /// The key of the property that identifies a request and the copies made for its retries.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="Guid"/>.
        /// </remarks>
        public const string TransactionId = nameof(HttpRestClient) + "." + nameof(TransactionId);

        /// <summary>
        /// The key of the property that holds the type into which the response is read.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="Type"/>.
        /// </remarks>
        public const string ResponseObjectType = nameof(HttpRestClient) + "." + nameof(ResponseObjectType);

        /// <summary>
        /// The key of the property that holds the <see cref="IHttpErrorHandler"/> that decided to retry the request after an error response.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="IHttpErrorHandler"/>.
        /// </remarks>
        public const string ErrorHandler = nameof(HttpRestClient) + "." + nameof(ErrorHandler);

        /// <summary>
        /// The key of the property that, when <see langword="true"/>, stops <see cref="ErrorHandlers.HttpError401Handler"/> from handling a
        /// '401 Unauthorized' response to the request.
        /// </summary>
        /// <remarks>
        /// The value of this property is of type <see cref="bool"/>.
        /// </remarks>
        public const string SkipUnauthorizedHandling = nameof(HttpRestClient) + "." + nameof(SkipUnauthorizedHandling);
    }
}
