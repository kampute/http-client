// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles '401 Unauthorized' responses by obtaining new authorization and retrying the request with it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a request receives a '401 Unauthorized' response, the handler calls the authentication function passed to its constructor, sets the
    /// authorization it returns as the <c>Authorization</c> header of <see cref="HttpRestClient.DefaultRequestHeaders"/>, and retries the request
    /// with it. The handler retries a call once: if the retry is also rejected, the response reaches the caller as an <see cref="HttpResponseException"/>.
    /// It does not retry a request whose content cannot be sent again, such as a <see cref="System.Net.Http.StreamContent"/> over a non-seekable
    /// stream, and does not call the function for it.
    /// </para>
    /// <para>
    /// When several requests are rejected at the same time, the function runs once and all of them retry with its result. A request that was rejected
    /// with older authorization than the latest one the handler obtained is retried with the latest one, without calling the function.
    /// </para>
    /// <para>
    /// One instance can serve several clients that use the same credentials. Keep it alive while the clients use it, and dispose it afterwards.
    /// </para>
    /// </remarks>
    /// <example>
    /// This handler obtains a new access token when a request is rejected. <c>RefreshAccessTokenAsync</c> stands for the application's own code that
    /// requests a token from its authentication service.
    /// <code>
    /// using var unauthorizedHandler = new HttpError401Handler(async (ctx, cancellationToken) =>
    /// {
    ///     var token = await RefreshAccessTokenAsync(cancellationToken);
    ///     return new AuthenticationHeaderValue(AuthSchemes.Bearer, token);
    /// });
    ///
    /// client.ErrorHandlers.Add(unauthorizedHandler);
    /// </code>
    /// </example>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public class HttpError401Handler : IHttpErrorHandler, IDisposable
    {
        private readonly Func<HttpResponseErrorContext, CancellationToken, Task<AuthenticationHeaderValue?>> _asyncAuthenticator;
        private readonly AsyncUpdateThrottle<AuthenticationHeaderValue?> _lastAuthorization;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpError401Handler"/> class.
        /// </summary>
        /// <param name="asyncAuthenticator">
        /// The function that obtains new authorization. It receives the context of the '401 Unauthorized' response and a cancellation token, and
        /// returns the <see cref="AuthenticationHeaderValue"/> to send, or <see langword="null"/> if authentication fails, in which case the request is
        /// not retried. Requests that the function sends with the same client are not handled by this handler, so a rejected token request cannot
        /// wait on itself.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncAuthenticator"/> is <see langword="null"/>.</exception>
        public HttpError401Handler(Func<HttpResponseErrorContext, CancellationToken, Task<AuthenticationHeaderValue?>> asyncAuthenticator)
        {
            _asyncAuthenticator = asyncAuthenticator ?? throw new ArgumentNullException(nameof(asyncAuthenticator));
            _lastAuthorization = new(null);
        }

        /// <summary>
        /// Determines whether this handler can process the specified HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns><see langword="true"/> if <paramref name="statusCode"/> is '401 Unauthorized'; otherwise, <see langword="false"/>.</returns>
        public bool CanHandle(HttpStatusCode statusCode) => statusCode == HttpStatusCode.Unauthorized;

        /// <summary>
        /// Returns the authorization to retry a rejected request with.
        /// </summary>
        /// <param name="ctx">The context of the '401 Unauthorized' response.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to the authorization to send, or to <see langword="null"/> if authentication failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If the request was sent with authorization other than the latest one the handler obtained, this method returns the latest one without calling
        /// the authentication function. Otherwise, it calls the function, or waits for a call already in progress.
        /// </remarks>
        protected virtual Task<AuthenticationHeaderValue?> AuthenticateAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            var currentAuthorization = _lastAuthorization.Value;
            if (currentAuthorization is not null && !currentAuthorization.Equals(ctx.Request.Headers.Authorization))
                return Task.FromResult<AuthenticationHeaderValue?>(currentAuthorization);

            return RefreshAuthorizationAsync(ctx, cancellationToken);
        }

        /// <summary>
        /// Invokes the authentication delegate, unless a concurrent call already did, and returns the most recently acquired authorization details.
        /// </summary>
        /// <param name="ctx">The error context for the HTTP response.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to the most recently acquired authorization details, or <see langword="null"/> if authentication failed.</returns>
        private async Task<AuthenticationHeaderValue?> RefreshAuthorizationAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            await _lastAuthorization.TryUpdateAsync(async () =>
            {
                using (ctx.Client.BeginPropertyScope(AuthorizationScope.Properties))
                {
                    return await _asyncAuthenticator(ctx, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);

            return _lastAuthorization.Value;
        }

        /// <inheritdoc/>
        async Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            if (ctx.Request.GetPropertyBag().TryGetValue(HttpRequestMessagePropertyKeys.SkipUnauthorizedHandling, out var skip) && skip is true)
                return HttpErrorHandlerResult.NoRetry;

            if (!ctx.Request.CanClone())
                return HttpErrorHandlerResult.NoRetry;

            var authorization = await AuthenticateAsync(ctx, cancellationToken).ConfigureAwait(false);
            if (authorization is null)
                return HttpErrorHandlerResult.NoRetry;

            var defaultHeaders = ctx.Client.DefaultRequestHeaders;
            lock (defaultHeaders)
            {
                if (!authorization.Equals(defaultHeaders.Authorization))
                    defaultHeaders.Authorization = authorization;
            }

            var authorizedRequest = ctx.Request.Clone();
            authorizedRequest.Headers.Authorization = authorization;
            authorizedRequest.GetPropertyBag()[HttpRequestMessagePropertyKeys.SkipUnauthorizedHandling] = true;
            return HttpErrorHandlerResult.Retry(authorizedRequest);
        }

        /// <summary>
        /// Releases the resources that the handler uses to coordinate concurrent authentication.
        /// </summary>
        public void Dispose()
        {
            _lastAuthorization.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Provides the request properties of the requests that the authentication function sends.
        /// </summary>
        /// <remarks>
        /// <see cref="HttpRequestMessagePropertyKeys.SkipUnauthorizedHandling"/> is set on those requests, so that a '401 Unauthorized' response to
        /// one of them does not start another authentication that would wait for the current one.
        /// </remarks>
        private static class AuthorizationScope
        {
            /// <summary>
            /// Gets the request properties of the requests that the authentication function sends.
            /// </summary>
            public static IEnumerable<KeyValuePair<string, object?>> Properties =>
            [
                new KeyValuePair<string, object?>(HttpRequestMessagePropertyKeys.SkipUnauthorizedHandling, true)
            ];
        }
    }
}
