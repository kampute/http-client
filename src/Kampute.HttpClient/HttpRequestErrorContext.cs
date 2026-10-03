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
    /// Represents the context of an HTTP request error, encapsulating details about the request, the error encountered, and the client that sent the request.
    /// </summary>
    public class HttpRequestErrorContext
    {
        /// <summary>
        /// Initializes an instance of the <see cref="HttpRequestErrorContext"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance used to send the request.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that resulted in a failure.</param>
        /// <param name="error">The <see cref="HttpRequestException"/> containing details of the error encountered during the HTTP request.</param>
        /// <param name="retryState">The retry budgets of the call that sent the request, shared by all its attempts.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="request"/>, <paramref name="error"/> or <paramref name="retryState"/> is <see langword="null"/>.</exception>
        public HttpRequestErrorContext(HttpRestClient client, HttpRequestMessage request, HttpRequestException error, HttpRetryState retryState)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            RetryState = retryState ?? throw new ArgumentNullException(nameof(retryState));
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
        /// Gets the retry budgets of the call that sent the request.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRetryState"/> shared by all attempts of the call. <see cref="ScheduleRetryAsync"/> keeps the retry scheduler of each source in it.
        /// </value>
        public HttpRetryState RetryState { get; }

        /// <summary>
        /// Schedules a retry for the failed HTTP request using a provided scheduler factory.
        /// </summary>
        /// <param name="source">
        /// The component that handles this kind of failure and owns its retry budget, such as the <see cref="HttpRestClient"/> for connection
        /// failures or an <see cref="IHttpErrorHandler"/> for error responses.
        /// </param>
        /// <param name="schedulerFactory">A function that returns an <see cref="IRetrySession"/> for scheduling retry attempts based on the error context.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/> indicating whether a retry should be attempted.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="schedulerFactory"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// Each source has its own retry budget for a call. The first time a source schedules a retry during a call, <paramref name="schedulerFactory"/>
        /// is called and the scheduler it returns is kept in <see cref="RetryState"/>. Later failures from the same source during the same call reuse
        /// that scheduler, so they share its budget, whether the request to retry is a clone of the failed request or a request built by an error handler.
        /// Failures from another source use another scheduler, so a request that fails in several ways can be retried more times in total than any single
        /// budget allows.
        /// </para>
        /// <para>
        /// If the request content cannot be sent again, the request is not retried and <paramref name="schedulerFactory"/> is not called.
        /// </para>
        /// </remarks>
        public Task<HttpErrorHandlerResult> ScheduleRetryAsync(object source, Func<HttpRequestErrorContext, IRetrySession?> schedulerFactory, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (schedulerFactory is null)
                throw new ArgumentNullException(nameof(schedulerFactory));

            if (!Request.CanClone())
                return Task.FromResult(HttpErrorHandlerResult.NoRetry);

            var session = RetryState.GetOrCreateSession(source, () => schedulerFactory(this));
            return session is not null
                ? RetryWhenScheduledAsync(session, cancellationToken)
                : Task.FromResult(HttpErrorHandlerResult.NoRetry);
        }

        /// <summary>
        /// Waits as the session decides, and returns a clone of the request to retry if the session allows another attempt.
        /// </summary>
        /// <param name="session">The retry session of the source that handles the failure.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/> indicating whether a retry should be attempted.</returns>
        private async Task<HttpErrorHandlerResult> RetryWhenScheduledAsync(IRetrySession session, CancellationToken cancellationToken)
        {
            return await session.WaitAsync(cancellationToken).ConfigureAwait(false)
                ? HttpErrorHandlerResult.Retry(Request.Clone())
                : HttpErrorHandlerResult.NoRetry;
        }
    }
}
