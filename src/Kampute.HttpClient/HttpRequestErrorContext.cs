// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the context of an HTTP request error, encapsulating details about the request, the error encountered, and the client that sent the request.
    /// </summary>
    public class HttpRequestErrorContext
    {
        /// <summary>
        /// Initializes an instance of the <see cref="HttpRequestErrorContext"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance used to send the request.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that resulted in a failure.</param>
        /// <param name="error">The <see cref="Exception"/> containing details of the error encountered during the HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="request"/> or <paramref name="error"/> or is <c>null</c>.</exception>
        public HttpRequestErrorContext(HttpRestClient client, HttpRequestMessage request, HttpRequestException error)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        /// <summary>
        /// Gets the <see cref="HttpRestClient"/> instance used to send the request.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRestClient"/> instance used to send the request.
        /// </value>
        public HttpRestClient Client { get; }

        /// <summary>
        /// Gets the <see cref="HttpRequestMessage"/> that resulted in a failure.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRequestMessage"/> that resulted in a failure.
        /// </value>
        public HttpRequestMessage Request { get; }

        /// <summary>
        /// Gets the <see cref="HttpRequestException"/> containing details of the error encountered during the HTTP request.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRequestException"/> containing details of the error encountered during the HTTP request.
        /// </value>
        public HttpRequestException Error { get; }

        /// <summary>
        /// Schedules a retry for the failed HTTP request using a provided scheduler factory.
        /// </summary>
        /// <param name="schedulerFactory">A function that returns an <see cref="IRetryScheduler"/> for scheduling retry attempts based on the error context.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/> indicating whether a retry should be attempted.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="schedulerFactory"/> is <c>null</c>.</exception>
        public async Task<HttpErrorHandlerResult> ScheduleRetryAsync(Func<HttpRequestErrorContext, IRetryScheduler?> schedulerFactory, CancellationToken cancellationToken = default)
        {
            if (schedulerFactory is null)
                throw new ArgumentNullException(nameof(schedulerFactory));

            if (!Request.CanClone())
                return HttpErrorHandlerResult.NoRetry;

            if (!Request.Properties.ContainsKey(HttpRequestMessagePropertyKeys.RetryScheduler))
                Request.Properties[HttpRequestMessagePropertyKeys.RetryScheduler] = schedulerFactory(this);

            var scheduler = Request.Properties[HttpRequestMessagePropertyKeys.RetryScheduler] as IRetryScheduler;

            return scheduler is not null && await scheduler.WaitAsync(cancellationToken).ConfigureAwait(false)
                ? HttpErrorHandlerResult.Retry(Request.Clone())
                : HttpErrorHandlerResult.NoRetry;
        }
    }
}
