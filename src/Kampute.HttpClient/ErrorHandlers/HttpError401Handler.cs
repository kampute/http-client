// Copyright (C) 2024 Kampute
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
    /// Handles '401 Unauthorized' HTTP responses by attempting to re-authenticate and retry the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="HttpError401Handler"/> class is specifically designed to enhance instances of <see cref="HttpRestClient"/> by providing a mechanism
    /// to handle HTTP '401 Unauthorized' responses. When a request made by a <see cref="HttpRestClient"/> instance receives a '401 Unauthorized' status code,
    /// this indicates that the request was rejected due to insufficient or missing authentication credentials. The <see cref="HttpError401Handler"/> responds 
    /// to such scenarios by initiating a re-authentication process using a delegate provided at instantiation, to obtain new authentication credentials.
    /// </para>
    /// <para>
    /// The delegate provided to the constructor is tasked with obtaining new authentication credentials, which might involve interacting with an authentication 
    /// server or prompting the user for credentials. Successful acquisition of new credentials leads to their application to the <see cref="HttpRestClient"/>
    /// instance, allowing the previously failed request to be retried with the updated authentication details.
    /// </para>
    /// <para>
    /// When an authentication process is underway for a client, subsequent authentication requests from the client will not initiate new processes. Instead, they 
    /// will await and utilize the outcome of the ongoing authentication. This approach guarantees that the authentication delegate is executed a single time for 
    /// concurrent requests, ensuring both efficiency and thread safety.
    /// </para>
    /// <para>
    /// A single instance of this error handler can be shared with multiple <see cref="HttpRestClient"/> instances, enabling centralized management of authentication
    /// challenges across various client instances that interact with different endpoints, if the <see cref="HttpRestClient"/> instances share the same authentication
    /// details. This enables a more efficient use of credentials and reduces the need for frequent re-authentications.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public class HttpError401Handler : IHttpErrorHandler, IDisposable
    {
        private readonly Func<HttpResponseErrorContext, CancellationToken, Task<AuthenticationHeaderValue?>> _asyncAuthenticator;
        private readonly AsyncUpdateThrottle<AuthenticationHeaderValue?> _lastAuthorization;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpError401Handler"/> class.
        /// </summary>
        /// <param name="asyncAuthenticator">
        /// The asynchronous delegate to be invoked to acquire new authorization details. The delegate receives the following parameters:
        /// <list type="bullet">
        ///   <item>
        ///     <term>context</term>
        ///     <description>
        ///     Provides context about the HTTP response indicating a '401 Unauthorized' error. It is encapsulated within
        ///     an <see cref="HttpResponseErrorContext"/> instance, allowing for an informed decision on authentication.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>cancellationToken</term>
        ///     <description>
        ///     A <see cref="CancellationToken"/> for canceling the operation.
        ///     </description>
        ///   </item>
        /// </list>
        /// The delegate should return a task resolving to an instance of <see cref="AuthenticationHeaderValue"/> containing the  authorization details
        /// necessary for subsequent requests if authentication can be successfully completed. If the authentication process fails, the delegate should
        /// return <c>null</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncAuthenticator"/> is <c>null</c>.</exception>
        public HttpError401Handler(Func<HttpResponseErrorContext, CancellationToken, Task<AuthenticationHeaderValue?>> asyncAuthenticator)
        {
            _asyncAuthenticator = asyncAuthenticator ?? throw new ArgumentNullException(nameof(asyncAuthenticator));
            _lastAuthorization = new(null);
        }

        /// <summary>
        /// Determines whether this handler can process the specified HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns><c>true</c> if the handler can process the status code; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This implementation specifically handles the HTTP '401 Unauthorized' status code.
        /// </remarks>
        public bool CanHandle(HttpStatusCode statusCode) => statusCode == HttpStatusCode.Unauthorized;

        /// <summary>
        /// Asynchronously authenticates an HTTP request that resulted in a '401 Unauthorized' response.
        /// </summary>
        /// <param name="ctx">The error context for the HTTP response.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="AuthenticationHeaderValue"/> if the client successfully acquires new authorization details; otherwise, <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="ctx"/> is <c>null</c>.</exception>
        protected virtual async Task<AuthenticationHeaderValue?> AuthenticateAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            await _lastAuthorization.TryUpdateAsync(async () =>
            {
                using (ctx.Client.BeginPropertyScope(new Dictionary<string, object> { [HttpRequestMessagePropertyKeys.SkipUnauthorizedHandling] = true }))
                {
                    return await _asyncAuthenticator(ctx, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);

            return _lastAuthorization.Value;
        }

        /// <inheritdoc/>
        async Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            if (ctx.Request.Properties.ContainsKey(HttpRequestMessagePropertyKeys.SkipUnauthorizedHandling))
                return HttpErrorHandlerResult.NoRetry;


            var authorization = await AuthenticateAsync(ctx, cancellationToken).ConfigureAwait(false);
            if (authorization is null)
                return HttpErrorHandlerResult.NoRetry;

            ctx.Client.DefaultRequestHeaders.Authorization = authorization;

            var authorizedRequest = ctx.Request.Clone();
            authorizedRequest.Headers.Authorization = authorization;
            authorizedRequest.Properties[HttpRequestMessagePropertyKeys.SkipUnauthorizedHandling] = true;
            return HttpErrorHandlerResult.Retry(authorizedRequest);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="HttpError401Handler"/> and optionally disposes of the managed resources.
        /// </summary>
        public void Dispose() => _lastAuthorization.Dispose();
    }
}
