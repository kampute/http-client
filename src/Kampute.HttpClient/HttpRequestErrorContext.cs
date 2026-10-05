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
    /// Describes a failed request to the code that decides whether to retry it: the client, the request, and the error.
    /// </summary>
    public class HttpRequestErrorContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestErrorContext"/> class.
        /// </summary>
        /// <param name="client">The client that sent the request.</param>
        /// <param name="request">The request that failed.</param>
        /// <param name="error">The exception that describes the failure.</param>
        /// <param name="retryState">The retry state of the call that sent the request, shared by all its attempts.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="request"/>, <paramref name="error"/> or <paramref name="retryState"/> is <see langword="null"/>.</exception>
        public HttpRequestErrorContext(HttpRestClient client, HttpRequestMessage request, HttpRequestException error, HttpRetryState retryState)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            RetryState = retryState ?? throw new ArgumentNullException(nameof(retryState));
        }

        /// <summary>
        /// Gets the client that sent the request.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRestClient"/> that sent the request.
        /// </value>
        public HttpRestClient Client { get; }

        /// <summary>
        /// Gets the request that failed.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRequestMessage"/> that failed.
        /// </value>
        public HttpRequestMessage Request { get; }

        /// <summary>
        /// Gets the exception that describes the failure.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRequestException"/> of the failure.
        /// </value>
        public HttpRequestException Error { get; }

        /// <summary>
        /// Gets the retry state of the call that sent the request.
        /// </summary>
        /// <value>
        /// The <see cref="HttpRetryState"/> shared by all attempts of the call. <see cref="ScheduleRetryAsync"/> keeps the retry session of each source in it.
        /// </value>
        public HttpRetryState RetryState { get; }

        /// <summary>
        /// Waits as the retry session of the source decides, and returns a clone of the request to retry, or a decision not to retry.
        /// </summary>
        /// <param name="source">
        /// The component that handles this kind of failure and counts its retries, such as the <see cref="HttpRestClient"/> for connection
        /// failures or an <see cref="IHttpErrorHandler"/> for error responses.
        /// </param>
        /// <param name="sessionFactory">
        /// A function that creates the retry session of the source from this context, or returns <see langword="null"/> if the source does not retry.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that resolves, after the wait, to a decision to retry with a clone of <see cref="Request"/>, or to <see cref="HttpErrorHandlerResult.NoRetry"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="sessionFactory"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// Each source has its own retry session for a call. The first time a source schedules a retry during a call, <paramref name="sessionFactory"/>
        /// is called and the session it returns is kept in <see cref="RetryState"/>. Later failures from the same source during the same call reuse
        /// that session, so its retry limit and elapsed time cover all of them, whether the request to retry is a clone of the failed request or a
        /// request built by an error handler. Failures from another source use another session, so a call that fails in several ways can be retried
        /// more times in total than any one session allows.
        /// </para>
        /// <para>
        /// If the request content cannot be sent again, the request is not retried and <paramref name="sessionFactory"/> is not called.
        /// </para>
        /// </remarks>
        public Task<HttpErrorHandlerResult> ScheduleRetryAsync(object source, Func<HttpRequestErrorContext, IRetrySession?> sessionFactory, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (sessionFactory is null)
                throw new ArgumentNullException(nameof(sessionFactory));

            if (!Request.CanClone())
                return Task.FromResult(HttpErrorHandlerResult.NoRetry);

            var session = RetryState.GetOrCreateSession(source, () => sessionFactory(this));
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
            return await session.WaitToRetryAsync(cancellationToken).ConfigureAwait(false)
                ? HttpErrorHandlerResult.Retry(Request.Clone())
                : HttpErrorHandlerResult.NoRetry;
        }
    }
}
