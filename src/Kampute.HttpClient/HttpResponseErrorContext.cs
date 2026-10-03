// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.Retry;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the context of an HTTP response error, providing information about the HTTP request, the client that sent the request, 
    /// the error encountered, and the response that indicates failure.
    /// </summary>
    public class HttpResponseErrorContext : HttpRequestErrorContext
    {
        /// <summary>
        /// Initializes an instance of the <see cref="HttpResponseErrorContext"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance used to send the request.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that resulted in a failure.</param>
        /// <param name="response">The <see cref="HttpResponseMessage"/> indicating the failure.</param>
        /// <param name="error">The <see cref="HttpResponseException"/> containing details of the error encountered during the HTTP request.</param>
        /// <param name="retryState">The retry budgets of the call that sent the request, shared by all its attempts.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="request"/>, <paramref name="response"/>, <paramref name="error"/> or <paramref name="retryState"/> is <see langword="null"/>.</exception>
        public HttpResponseErrorContext(HttpRestClient client, HttpRequestMessage request, HttpResponseMessage response, HttpResponseException error, HttpRetryState retryState)
            : base(client, request, error, retryState)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/> indicating the failure.
        /// </summary>
        /// <value>
        /// The <see cref="HttpResponseMessage"/> indicating the failure.
        /// </value>
        public HttpResponseMessage Response { get; }

        /// <summary>
        /// Gets the <see cref="HttpResponseException"/> containing details of the HTTP response error.
        /// </summary>
        /// <value>
        /// The <see cref="HttpResponseException"/> containing details of the HTTP response error.
        /// </value>
        public new HttpResponseException Error => (HttpResponseException)base.Error;

        /// <summary>
        /// Schedules a retry for the failed HTTP request using a retry session from the provided factory.
        /// </summary>
        /// <param name="source">
        /// The component that handles this kind of failure and owns its retry budget, typically the <see cref="IHttpErrorHandler"/> that
        /// handles the response.
        /// </param>
        /// <param name="sessionFactory">A function that returns an <see cref="IRetrySession"/> that decides on retry attempts, based on the error context.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/> indicating whether a retry should be attempted.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="sessionFactory"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Each source has its own retry budget for a call, as described for
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
