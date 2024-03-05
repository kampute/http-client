// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Manages clones of <see cref="HttpRequestMessage"/> for retry operations.
    /// </summary>
    /// <remarks>
    /// This class oversees the life-cycle of cloned <see cref="HttpRequestMessage"/> instances. It ensures that clones are properly managed 
    /// and disposed of, preventing resource leaks and maintaining the integrity of the original <see cref="HttpRequestMessage"/>.
    /// </remarks>
    public struct HttpRequestMessageCloneManager : IDisposable
    {
        private readonly HttpRequestMessage _originalRequest;
        private HttpRequestMessage _currentRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestMessageCloneManager"/> struct with the specified HTTP request.
        /// </summary>
        /// <param name="request">The original <see cref="HttpRequestMessage"/> to send.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HttpRequestMessageCloneManager(HttpRequestMessage request)
        {
            _originalRequest = request ?? throw new ArgumentNullException(nameof(request));
            _currentRequest = _originalRequest;
        }

        /// <summary>
        /// Gets the current <see cref="HttpRequestMessage"/> to send. This request may be the original or a new request based on retry decisions.
        /// </summary>
        public readonly HttpRequestMessage RequestToSend
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentRequest;
        }

        /// <summary>
        /// Attempts to apply a retry decision to the current request. If the decision includes a request to retry, updates the current request and 
        /// disposes of the previous request if it is not the original.
        /// </summary>
        /// <param name="decision">The retry decision.</param>
        /// <returns><c>true</c> if the decision was applied and a retry should occur; otherwise, <c>false</c>.</returns>
        public bool TryApplyDecision(in HttpErrorHandlerResult decision)
        {
            if (decision.RequestToRetry is null)
                return false;

            DisposeNonOriginalRequest();
            _currentRequest = decision.RequestToRetry;
            return true;
        }

        /// <summary>
        /// Disposes of any cloned requests, releasing the managed resources.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            DisposeNonOriginalRequest();
        }

        /// <summary>
        /// Disposes of the current request if it is not the original request.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void DisposeNonOriginalRequest()
        {
            if (!ReferenceEquals(_currentRequest, _originalRequest))
            {
                if (_currentRequest.IsCloned())
                    _currentRequest.Content = null; // Content is reused, not cloned.
                _currentRequest.Dispose();
            }
        }
    }
}
