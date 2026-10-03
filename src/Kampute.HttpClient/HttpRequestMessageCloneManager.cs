// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Manages the requests that replace the original <see cref="HttpRequestMessage"/> during retries.
    /// </summary>
    /// <remarks>
    /// Each request that replaces another is disposed when it is itself replaced or when the manager is disposed. The original request is never
    /// disposed. The content of a replaced request is disposed only when no other request still uses it: content that is the original request's
    /// content, or that the replacing request reuses, is left undisposed.
    /// </remarks>
    internal struct HttpRequestMessageCloneManager : IDisposable
    {
        private readonly HttpRequestMessage _originalRequest;
        private HttpRequestMessage _currentRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestMessageCloneManager"/> struct with the specified HTTP request.
        /// </summary>
        /// <param name="request">The original <see cref="HttpRequestMessage"/> to send.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is <see langword="null"/>.</exception>
        public HttpRequestMessageCloneManager(HttpRequestMessage request)
        {
            _originalRequest = request ?? throw new ArgumentNullException(nameof(request));
            _currentRequest = _originalRequest;
        }

        /// <summary>
        /// Gets the current <see cref="HttpRequestMessage"/> to send. This request may be the original or a new request based on retry decisions.
        /// </summary>
        /// <value>
        /// The current <see cref="HttpRequestMessage"/> to send.
        /// </value>
        public readonly HttpRequestMessage RequestToSend => _currentRequest;

        /// <summary>
        /// Attempts to apply a retry decision to the current request. If the decision includes a request to retry, makes it the current request and
        /// disposes of the request it replaces, unless that is the original request.
        /// </summary>
        /// <param name="decision">The retry decision.</param>
        /// <returns><see langword="true"/> if the decision was applied and a retry should occur; otherwise, <see langword="false"/>.</returns>
        public bool TryApplyDecision(in HttpErrorHandlerResult decision)
        {
            var nextRequest = decision.RequestToRetry;
            if (nextRequest is null)
                return false;

            if (!ReferenceEquals(nextRequest, _currentRequest))
            {
                DisposeCurrentRequest(contentInUse: nextRequest.Content);
                _currentRequest = nextRequest;
            }

            return true;
        }

        /// <summary>
        /// Disposes of the current request, unless it is the original request.
        /// </summary>
        public readonly void Dispose()
        {
            DisposeCurrentRequest(contentInUse: null);
        }

        /// <summary>
        /// Disposes of the current request if it is not the original request, leaving its content undisposed if another request still uses it.
        /// </summary>
        /// <param name="contentInUse">The content of the request that replaces the current request, if any.</param>
        private readonly void DisposeCurrentRequest(HttpContent? contentInUse)
        {
            if (ReferenceEquals(_currentRequest, _originalRequest))
                return;

            var content = _currentRequest.Content;
            if (content is not null && (ReferenceEquals(content, _originalRequest.Content) || ReferenceEquals(content, contentInUse)))
                _currentRequest.Content = null;

            _currentRequest.Dispose();
        }
    }
}
