// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.ErrorHandlers
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
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
    /// This error handler can be integrated with multiple <see cref="HttpRestClient"/> instances, enabling centralized management of authentication challenges 
    /// across various client instances that interact with different endpoints. It is crucial, therefore, that the delegate provided for authentication is designed 
    /// to be thread-safe, ensuring consistent and safe operation across concurrent authentication processes.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ErrorHandlers"/>
    public class HttpError401Handler : IHttpErrorHandler, IDisposable
    {
        private const string AlreadyRetried = nameof(HttpError401Handler) + "." + nameof(AlreadyRetried);

        private readonly ConcurrentDictionary<HttpRestClient, AuthenticationState> _authenticationStates = new();
        private readonly Func<HttpRestClient, IEnumerable<AuthenticationHeaderValue>, CancellationToken, Task<AuthenticationHeaderValue?>> _asyncAuthenticator;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpError401Handler"/> class.
        /// </summary>
        /// <param name="asyncAuthenticator">The asynchronous delegate to be invoked to acquire new authorization details.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncAuthenticator"/> is <c>null</c>.</exception>
        /// <remarks>
        /// <para>
        /// The <paramref name="asyncAuthenticator"/> delegate is responsible for obtaining new authentication credentials, typically in response to an 
        /// authentication challenge indicated by an HTTP '401 Unauthorized' status code.
        /// </para>
        /// <para>
        /// The delegate should evaluate the provided challenges and return an instance of <see cref="AuthenticationHeaderValue"/> containing the 
        /// authorization details necessary for subsequent requests if authentication can be successfully completed. If the authentication process 
        /// fails, or if no suitable challenge can be met, the delegate should return <c>null</c>.
        /// </para>
        /// <para>
        /// The delegate receives the following parameters:
        /// <list type="bullet">
        /// <item>
        /// <term>client</term>
        /// <description>The instance of <see cref="HttpRestClient"/> making the request that was challenged with a '401 Unauthorized' response. 
        /// This client can be used to perform further requests, such as retrieving a new token from an authentication server.</description>
        /// </item>
        /// <item>
        /// <term>challenges</term>
        /// <description>The collection of <see cref="AuthenticationHeaderValue"/> instances representing the server's authentication challenges.</description>
        /// </item>
        /// <item>
        /// <term>cancellationToken</term>
        /// <description>A <see cref="CancellationToken"/> for canceling the operation.</description>
        /// </item>
        /// </list>
        /// </para>
        /// <para>
        /// It is crucial to use the <see cref="HttpRestClient"/> provided to the delegate for any requests using the client that triggered the 
        /// authentication challenge, to prevent deadlocks caused by recurring 401 errors. This client is a temporary, cloned version of the original 
        /// client, created specifically for handling the authentication process. As such, any changes made to this clone's configuration will not 
        /// impact the original client. 
        /// </para>
        /// </remarks>
        public HttpError401Handler(Func<HttpRestClient, IEnumerable<AuthenticationHeaderValue>, CancellationToken, Task<AuthenticationHeaderValue?>> asyncAuthenticator)
        {
            _asyncAuthenticator = asyncAuthenticator ?? throw new ArgumentNullException(nameof(asyncAuthenticator));
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="HttpError401Handler"/> and optionally disposes of the managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
        /// Asynchronously attempts to authenticate the client by new authorization details.
        /// </summary>
        /// <param name="client">The instance of <see cref="HttpRestClient"/> that requires authentication.</param>
        /// <param name="scheme">The desired authorization scheme, such as "Bearer" or "Basic".</param>
        /// <param name="parameter">An optional parameter for the authorization scheme.</param>
        /// <returns><c>true</c> if authorization details are successfully acquired and set; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="scheme"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// Upon successful authentication, this method directly updates the default request headers of the <paramref name="client"/> 
        /// with the provided authorization details, ensuring that all subsequent requests from the <paramref name="client"/> use these 
        /// credentials.         
        /// </remarks>
        public virtual Task<bool> TryAuthenticateAsync(HttpRestClient client, string scheme, string? parameter = null)
        {
            if (string.IsNullOrEmpty(scheme))
                throw new ArgumentException($"'{nameof(scheme)}' cannot be null or empty.", nameof(scheme));

            return TrySolveAuthenticationChallengeAsync(client, [new AuthenticationHeaderValue(scheme, parameter)], CancellationToken.None);
        }

        /// <summary>
        /// Disposes the <see cref="HttpError401Handler"/> instance.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from a <see cref="IDisposable.Dispose()"/> method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var entry in _authenticationStates)
                {
                    entry.Key.Disposing -= ClientDisposing;
                    entry.Value.Dispose();
                }
            }
        }

        /// <summary>
        /// Attempts to solve at least one of the authentication challenges received from the server.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance attempting to authenticate.</param>
        /// <param name="challenges">A collection of <see cref="AuthenticationHeaderValue"/> items representing the <c>WWW-Authenticate</c> headers received from a server response.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to <c>true</c> if the client successfully acquires and sets new authorization details; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="client"/> or <paramref name="challenges"/> is <c>null</c>.</exception>
        protected virtual async Task<bool> TrySolveAuthenticationChallengeAsync
        (
            HttpRestClient client,
            IEnumerable<AuthenticationHeaderValue> challenges,
            CancellationToken cancellationToken
        )
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));
            if (challenges is null)
                throw new ArgumentNullException(nameof(challenges));

            var state = _authenticationStates.GetOrAdd(client, owner =>
            {
                owner.Disposing += ClientDisposing;
                return new AuthenticationState();
            });

            if (await state.TryAcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                state.LastAuthenticationResult = false;
                try
                {
                    // To avoid deadlock, we use another client without this instance of UnauthorizedHttpErrorHandler.
                    // This is necessary because invoking the OnAuthenticationChallenge delegate could potentially lead to a recursive situation
                    // where an authentication request itself returns a 401 Unauthorized response. If the original client with the unauthorized error
                    // handler is used for this authentication request, it could trigger the error handling mechanism again, leading to a deadlock
                    // as the system waits indefinitely for the authentication process to complete. By cloning the client and removing this
                    // UnauthorizedHttpErrorHandler from the list of error handlers, we ensure that authentication requests made within the delegate
                    // do not re-enter the error handling process, thus preventing a deadlock situation. The helper client is a temporary
                    // solution used solely for the purpose of authentication and is disposed of after use to ensure resource cleanup.
                    using var helperClient = (HttpRestClient)client.Clone();
                    helperClient.ErrorHandlers.Remove(this);

                    var authorization = await _asyncAuthenticator(helperClient, challenges, cancellationToken).ConfigureAwait(false);
                    if (authorization is not null)
                    {
                        client.DefaultRequestHeaders.Authorization = authorization;
                        state.LastAuthenticationResult = true;
                    }
                }
                finally
                {
                    state.Release();
                }
            }
            return state.LastAuthenticationResult;
        }

        /// <summary>
        /// Attempts to recover from an unauthorized request error by re-authenticating and retrying the request.
        /// </summary>
        /// <param name="ctx">The context containing information about the HTTP response that indicates a failure.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>.</returns>
        /// <exception cref="ArgumentNullException">Throws if <paramref name="ctx"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method attempts to re-authenticate the client in response to an '401 Unauthorized'. The process involves:
        /// <list type="bullet">
        /// <item>
        /// <description>Invoking the provided asynchronous delegate to obtain new authorization details.</description>
        /// </item>
        /// <item>
        /// <description>If authentication is successful, the method clones the original request, updates it with the new authorization 
        /// header, and returns <see cref="HttpErrorHandlerResult.Retry(HttpRequestMessage)"/> with the updated request, indicating that 
        /// the request should be retried.</description>
        /// </item>
        /// <item>
        /// <description>If authentication cannot be successfully completed or if no suitable challenge can be met, the method returns 
        /// <see cref="HttpErrorHandlerResult.NoRetry"/>, indicating that the request cannot be recovered by this handler.</description>
        /// </item>
        /// </list>
        /// </remarks>
        protected virtual async Task<HttpErrorHandlerResult> DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            if (ctx.Request.Properties.ContainsKey(AlreadyRetried) || !ctx.Request.CanClone())
                return HttpErrorHandlerResult.NoRetry;

            if (!await TrySolveAuthenticationChallengeAsync(ctx.Client, ctx.Response.Headers.WwwAuthenticate, cancellationToken).ConfigureAwait(false))
                return HttpErrorHandlerResult.NoRetry;

            var authorizedRequest = ctx.Request.Clone();
            authorizedRequest.Properties[AlreadyRetried] = true;
            authorizedRequest.Headers.Authorization = ctx.Client.DefaultRequestHeaders.Authorization;
            return HttpErrorHandlerResult.Retry(authorizedRequest);
        }

        /// <inheritdoc/>
        Task<HttpErrorHandlerResult> IHttpErrorHandler.DecideOnRetryAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            return DecideOnRetryAsync(ctx, cancellationToken);
        }

        private void ClientDisposing(object sender, EventArgs e)
        {
            _authenticationStates.TryRemove((HttpRestClient)sender, out _);
        }

        #region Helper Class

        /// <summary>
        /// Represents the authentication state for an instance of <see cref="HttpRestClient"/>.
        /// </summary>
        /// <remarks>
        /// This private class is designed to manage and encapsulate the state of authentication for a specific <see cref="HttpRestClient"/>.
        /// It provides mechanisms to track whether authentication is currently being attempted , as well as whether the client has been 
        /// successfully authenticated. It ensures that authentication attempts are synchronized, preventing concurrent authentication 
        /// attempts on the same client instance which could lead to race conditions or other unintended behaviors.
        /// </remarks>
        private class AuthenticationState : IDisposable
        {
            private readonly SemaphoreSlim _semaphore = new(1, 1);
            private volatile bool _authenticating;
            private volatile bool _authenticated;

            /// <summary>
            /// Gets or sets a value indicating whether the client was successfully authenticated on the last authentication attempt.
            /// </summary>
            /// <value>
            /// A <see cref="bool"/> value indicating whether the client was successfully authenticated on the last authentication attempt.
            /// </value>
            public bool LastAuthenticationResult
            {
                get => _authenticated;
                set => _authenticated = value;
            }

            /// <summary>
            /// Asynchronously attempts to acquire the authentication lock, indicating that an authentication process is starting.
            /// </summary>
            /// <param name="cancellationToken">A token for canceling the operation.</param>
            /// <returns>A task that resolves to <c>true</c> if the lock was successfully acquired and authentication should proceed;
            /// otherwise, <c>false</c> if another authentication process is already underway.</returns>
            /// <remarks>
            /// This method ensures that only one authentication process can be active at a time for a given client instance. If
            /// the lock is successfully acquired, it indicates that no other authentication process is currently underway for this
            /// client, and the caller can proceed with authentication.
            /// </remarks>
            public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
            {
                _authenticating = true;
                await _semaphore.WaitAsync(cancellationToken);
                if (_authenticating)
                    return true;

                _semaphore.Release();
                return false;
            }

            /// <summary>
            /// Releases the authentication lock, indicating that the authentication process has completed.
            /// </summary>
            /// <remarks>
            /// This method should be called when an authentication attempt has finished, regardless of its outcome. It signals
            /// that the current authentication process is complete, allowing other authentication attempts to proceed.
            /// </remarks>
            public void Release()
            {
                _authenticating = false;
                _semaphore.Release();
            }

            /// <summary>
            /// Releases all resources used by the <see cref="AuthenticationState"/>.
            /// </summary>
            public void Dispose()
            {
                _semaphore.Dispose();
            }
        }

        #endregion
    }
}
