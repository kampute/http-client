// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers.Abstracts;
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Handles responses with a transient error status code by retrying the request after a delay.
    /// </summary>
    /// <remarks>
    /// If the first error response of a call that this handler handles has a <c>Retry-After</c> header, the request is retried once, at the time
    /// the header suggests; a repeated error response then reaches the caller. Without the header, the request is retried as the
    /// <see cref="HttpRestClient.RetryPolicy"/> of the client decides. <see cref="RetryableHttpErrorHandler.OnRetryPolicy"/> can choose another policy.
    /// If a suggested time is further away than <see cref="RetryableHttpErrorHandler.MaxRetryDelay"/>, which is five minutes by default, the request is
    /// not retried. See <see cref="RetryableHttpErrorHandler"/> for how the retries of a call are decided.
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    /// <seealso cref="HttpRestClient.RetryPolicy"/>
    public class TransientHttpErrorHandler : RetryableHttpErrorHandler
    {
        private readonly HashSet<HttpStatusCode> _handledStatusCodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientHttpErrorHandler"/> class with default transient error status codes.
        /// </summary>
        /// <remarks>
        /// The default transient error status codes handled by this instance are:
        /// <list type="bullet">
        ///   <item><term>408</term><description>Request Timeout</description></item>
        ///   <item><term>502</term><description>Bad Gateway</description></item>
        ///   <item><term>503</term><description>Service Unavailable</description></item>
        ///   <item><term>504</term><description>Gateway Timeout</description></item>
        ///   <item><term>507</term><description>Insufficient Storage</description></item>
        ///   <item><term>509</term><description>Bandwidth Limit Exceeded</description></item>
        /// </list>
        /// </remarks>
        public TransientHttpErrorHandler()
        {
            _handledStatusCodes =
            [
                HttpStatusCode.RequestTimeout,     // 408 - Request Timeout
                HttpStatusCode.BadGateway,         // 502 - Bad Gateway
                HttpStatusCode.ServiceUnavailable, // 503 - Service Unavailable
                HttpStatusCode.GatewayTimeout,     // 504 - Gateway Timeout
                (HttpStatusCode) 507,              // 507 - Insufficient Storage
                (HttpStatusCode) 509,              // 509 - Bandwidth Limit Exceeded
            ];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientHttpErrorHandler"/> class with specified transient error status codes.
        /// </summary>
        /// <param name="statusCodes">The collection of HTTP status codes that this handler will process as transient errors.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="statusCodes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="statusCodes"/> is empty.</exception>
        public TransientHttpErrorHandler(IEnumerable<HttpStatusCode> statusCodes)
        {
            if (statusCodes is null)
                throw new ArgumentNullException(nameof(statusCodes));

            _handledStatusCodes = [.. statusCodes];
            if (_handledStatusCodes.Count == 0)
                throw new ArgumentException("At least one status code must be provided.", nameof(statusCodes));
        }

        /// <summary>
        /// Gets the collection of HTTP status codes that are considered transient errors and handled by this instance.
        /// </summary>
        /// <value>
        /// A read-only collection of <see cref="HttpStatusCode"/> values that are treated as transient errors.
        /// </value>
        public IReadOnlyCollection<HttpStatusCode> HandledStatusCodes => _handledStatusCodes;

        /// <inheritdoc/>
        public sealed override bool CanHandle(HttpStatusCode statusCode) => _handledStatusCodes.Contains(statusCode);
    }
}
