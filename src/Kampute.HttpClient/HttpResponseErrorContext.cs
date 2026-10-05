// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.Resilience;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Describes an error response to the error handlers that decide whether to retry the request: the client, the request, the response, and the
    /// error.
    /// </summary>
    public class HttpResponseErrorContext : HttpRequestErrorContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseErrorContext"/> class.
        /// </summary>
        /// <param name="client">The client that sent the request.</param>
        /// <param name="request">The request that received the error response.</param>
        /// <param name="response">The error response.</param>
        /// <param name="error">The exception for the error response.</param>
        /// <param name="retryState">The retry state of the call that sent the request, shared by all its attempts.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="request"/>, <paramref name="response"/>, <paramref name="error"/> or <paramref name="retryState"/> is <see langword="null"/>.</exception>
        public HttpResponseErrorContext(HttpRestClient client, HttpRequestMessage request, HttpResponseMessage response, HttpResponseException error, HttpRetryState retryState)
            : base(client, request, error, retryState)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        /// <summary>
        /// Gets the error response.
        /// </summary>
        /// <value>
        /// The <see cref="HttpResponseMessage"/> with the error status code.
        /// </value>
        public HttpResponseMessage Response { get; }

        /// <summary>
        /// Gets the exception for the error response.
        /// </summary>
        /// <value>
        /// The <see cref="HttpResponseException"/> that the client throws if no handler retries the request.
        /// </value>
        public new HttpResponseException Error => (HttpResponseException)base.Error;

        /// <summary>
        /// Waits as the retry session of the source decides, and returns a clone of the request to retry, or a decision not to retry.
        /// </summary>
        /// <param name="source">
        /// The component that handles this kind of failure and counts its retries, typically the <see cref="IHttpErrorHandler"/> that
        /// handles the response.
        /// </param>
        /// <param name="sessionFactory">
        /// A function that creates the retry session of the source from this context, or returns <see langword="null"/> if the source does not retry.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that resolves, after the wait, to a decision to retry with a clone of the request, or to <see cref="HttpErrorHandlerResult.NoRetry"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="sessionFactory"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Each source has its own retry session for a call, as described for
        /// <see cref="HttpRequestErrorContext.ScheduleRetryAsync(object, Func{HttpRequestErrorContext, IRetrySession}, CancellationToken)"/>.
        /// </remarks>
        public Task<HttpErrorHandlerResult> ScheduleRetryAsync(object source, Func<HttpResponseErrorContext, IRetrySession?> sessionFactory, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (sessionFactory is null)
                throw new ArgumentNullException(nameof(sessionFactory));

            return base.ScheduleRetryAsync(source, _ => sessionFactory(this), cancellationToken);
        }
    }
}
