// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Content;
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Sends requests to a REST API through an <see cref="HttpClient"/>, and converts between .NET objects and HTTP content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, every <see cref="HttpRestClient"/> uses one shared <see cref="HttpClient"/>, so clients created for different APIs reuse the same
    /// connections. A client can also use an <see cref="HttpClient"/> that the application creates and configures.
    /// </para>
    /// <para>
    /// <see cref="DefaultRequestHeaders"/> apply to every request of the client. <see cref="BeginHeaderScope"/> and <see cref="BeginPropertyScope"/>
    /// add or override headers and request properties until the scope is disposed, without changing the defaults.
    /// </para>
    /// <para>
    /// <see cref="ContentFormatters"/> read response content into .NET objects by its <c>Content-Type</c>, and write the payloads of
    /// <see cref="HttpRestClientExtensions.SendObjectAsync{T}(HttpRestClient, HttpMethod, string, object, string, CancellationToken)"/>. When a request
    /// sets no <c>Accept</c> header, the client sets one from the media types the formatters can read into the expected type.
    /// </para>
    /// <para>
    /// Failures are recovered in two ways. <see cref="RetryPolicy"/> decides whether to retry after a transient connection failure or a timeout, and
    /// <see cref="ErrorHandlers"/> decide whether to retry after an error response. An error response that no handler retries is thrown as an
    /// <see cref="HttpResponseException"/>.
    /// </para>
    /// <para>
    /// <see cref="BeforeSendingRequest"/> and <see cref="AfterReceivingResponse"/> let the application change or inspect each request and response,
    /// for example to add headers or to log.
    /// </para>
    /// </remarks>
    public class HttpRestClient : IDisposable
    {
        private static HttpRequestHeaders CreateRequestHeaders()
        {
            using var request = new HttpRequestMessage();
            return request.Headers;
        }

        private readonly HttpClient _httpClient;
        private readonly IDisposable? _disposable;

        private readonly ScopedCollection<KeyValuePair<string, string?>> _scopedHeaders = new();
        private readonly ScopedCollection<KeyValuePair<string, object?>> _scopedProperties = new();

        private IHttpRetryPolicy _retryPolicy = HttpRetryPolicy.None;
        private Uri? _baseAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRestClient"/> class that uses the shared <see cref="HttpClient"/>.
        /// </summary>
        /// <remarks>
        /// The client acquires a reference to the <see cref="HttpClient"/> of <see cref="SharedHttpClient"/> and releases it when it is disposed.
        /// The shared <see cref="HttpClient"/> is disposed when its last reference is released.
        /// </remarks>
        public HttpRestClient()
            : this(SharedHttpClient.AcquireReference())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRestClient"/> class that uses a shared <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClientReference">
        /// A reference to the shared <see cref="HttpClient"/>. The client takes ownership of the reference and releases it when it is disposed.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpClientReference"/> is <see langword="null"/>.</exception>
        public HttpRestClient(SharedDisposable<HttpClient>.Reference httpClientReference)
        {
            if (httpClientReference is null)
                throw new ArgumentNullException(nameof(httpClientReference));

            _httpClient = httpClientReference.Instance;
            _disposable = httpClientReference;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRestClient"/> class with the specified <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> that sends the requests of this client.</param>
        /// <param name="disposeClient">
        /// <see langword="true"/> to dispose <paramref name="httpClient"/> when this client is disposed; <see langword="false"/> to leave it to its owner.
        /// The default is <see langword="true"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpClient"/> is <see langword="null"/>.</exception>
        public HttpRestClient(HttpClient httpClient, bool disposeClient = true)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (disposeClient)
                _disposable = httpClient;
        }

        /// <summary>
        /// Occurs before a request is sent.
        /// </summary>
        /// <remarks>
        /// The event is raised before every attempt of a request, including retries. Handlers can change the request, for example to add a header,
        /// and the changes are sent.
        /// </remarks>
        public event EventHandler<HttpRequestMessageEventArgs>? BeforeSendingRequest;

        /// <summary>
        /// Occurs when an HTTP response has been received.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The event is raised for every response, including error responses and responses that lead to a retry, before the client checks the status
        /// code or reads the content. Handlers can inspect or log the response.
        /// </para>
        /// <para>
        /// For requests sent with <see cref="HttpCompletionOption.ResponseHeadersRead"/>, such as those of <see cref="HttpRestClientExtensions.GetAsStreamAsync"/>,
        /// <see cref="HttpRestClientExtensions.GetToStreamAsync"/> and <see cref="HttpRestClientExtensions.DownloadAsync"/>, the event is raised once the headers arrive
        /// and the body is not buffered. A subscriber that reads the response content consumes the body stream, which leaves nothing for the caller.
        /// </para>
        /// </remarks>
        public event EventHandler<HttpResponseMessageEventArgs>? AfterReceivingResponse;

        /// <summary>
        /// Occurs when the client is disposed, before it releases its <see cref="HttpClient"/>.
        /// </summary>
        public event EventHandler<EventArgs>? Disposing;

        /// <summary>
        /// Gets or sets the base address of the requests.
        /// </summary>
        /// <value>
        /// The URI that relative request URIs are resolved against, or <see langword="null"/> if requests use absolute URIs.
        /// </value>
        /// <remarks>
        /// <para>
        /// If the base address does not end with a slash, one is appended. Without it, the last segment of the path would be replaced when a relative
        /// URI is resolved: with <c>http://example.com/api</c>, the relative URI <c>users</c> would resolve to <c>http://example.com/users</c> instead
        /// of <c>http://example.com/api/users</c>.
        /// </para>
        /// <para>
        /// When the base address is <see langword="null"/>, every request must use an absolute URI.
        /// </para>
        /// </remarks>
        public Uri? BaseAddress
        {
            get => _baseAddress;
            set => _baseAddress = value is null || value.AbsolutePath.EndsWith("/") ? value : new Uri(value.GetLeftPart(UriPartial.Path) + "/");
        }

        /// <summary>
        /// Gets or sets the retry policy for transient connection failures.
        /// </summary>
        /// <value>
        /// The <see cref="IHttpRetryPolicy"/> that decides whether and when a request is retried after a transient connection failure or a timeout.
        /// The default is <see cref="HttpRetryPolicy.None"/>, which does not retry; setting <see langword="null"/> restores it.
        /// </value>
        /// <remarks>
        /// <para>
        /// The policy applies when a request fails without a response, for example because the connection is refused, reset, or closed before the
        /// response is complete, the host name cannot be resolved or the host cannot be reached, or <see cref="System.Net.Http.HttpClient.Timeout"/>
        /// elapses. Error responses, such as '503 Service Unavailable', are handled by <see cref="ErrorHandlers"/> instead.
        /// </para>
        /// <para>
        /// The limits of the policy apply to the retries after connection failures only. Error handlers count their own retries separately, so a
        /// call that fails in several ways can be retried more times in total than this policy allows.
        /// </para>
        /// </remarks>
        /// <example>
        /// This policy retries a request up to five times after connection failures, with a delay that starts at one second and doubles each time:
        /// <code>
        /// client.RetryPolicy = RetryStrategies.Exponential(TimeSpan.FromSeconds(1)).WithMaxRetries(5).ToHttpRetryPolicy();
        /// </code>
        /// </example>
        public IHttpRetryPolicy RetryPolicy
        {
            get => _retryPolicy;
            set => _retryPolicy = value ?? HttpRetryPolicy.None;
        }

        /// <summary>
        /// Gets or sets the type into which the body of an error response is read.
        /// </summary>
        /// <value>
        /// The error model of the API, or <see langword="null"/> to leave the body of error responses unread.
        /// </value>
        /// <remarks>
        /// <para>
        /// When this property is set, the client reads the body of an error response into this type with the formatters in
        /// <see cref="ContentFormatters"/>, and exposes the result through <see cref="HttpResponseException.ResponseObject"/>. If the body cannot be
        /// read, <see cref="HttpResponseException.ResponseObject"/> is <see langword="null"/>.
        /// </para>
        /// <para>
        /// When the type implements <see cref="IHttpErrorResponse"/>, the object read from the body creates the exception, so it can carry the error
        /// details of the API, such as validation errors.
        /// </para>
        /// </remarks>
        public Type? ResponseErrorType { get; set; }

        /// <summary>
        /// Gets the error handlers that decide whether to retry a request after an error response.
        /// </summary>
        /// <value>
        /// The error handlers of this client.
        /// </value>
        /// <remarks>
        /// When a request receives an error response, the handlers whose <see cref="IHttpErrorHandler.CanHandle"/> accepts the status code are asked in
        /// the order they were added, until one of them returns a request to retry. If none does, the error is thrown as an <see cref="HttpResponseException"/>.
        /// </remarks>
        public HttpErrorHandlerCollection ErrorHandlers { get; } = [];

        /// <summary>
        /// Gets the content formatters that read response content and write request payloads.
        /// </summary>
        /// <value>
        /// The mutable collection of <see cref="IHttpContentFormatter"/> instances of this client.
        /// </value>
        /// <remarks>
        /// <para>
        /// The formatters are tried in order. The first one that can read the media type of a response into the expected .NET type reads it, and the
        /// media types they can read feed the <c>Accept</c> header of requests that do not set one. The first one that can write a payload in the
        /// requested media type writes the payloads of <see cref="HttpRestClientExtensions.SendObjectAsync{T}(HttpRestClient, HttpMethod, string, object, string, CancellationToken)"/>.
        /// </para>
        /// <para>
        /// The collection is empty initially. Format packages register their formatters with extension methods such as <c>UseJson</c>.
        /// </para>
        /// </remarks>
        public HttpContentFormatterCollection ContentFormatters { get; } = [];

        /// <summary>
        /// Gets the headers sent with every request of this client.
        /// </summary>
        /// <value>
        /// The default request headers of this client.
        /// </value>
        /// <remarks>
        /// <para>
        /// Headers of an active scope, begun with <see cref="BeginHeaderScope"/>, override these headers.
        /// </para>
        /// <para>
        /// <see cref="HttpHeaders"/> is not thread-safe. The client locks this collection while it copies the headers into each new request, and
        /// <see cref="Kampute.HttpClient.ErrorHandlers.HttpError401Handler"/> locks it while it updates the <c>Authorization</c> header. Code that
        /// changes this collection while requests are in flight must lock the same object.
        /// </para>
        /// </remarks>
        /// <example>
        /// This code replaces the access token while other requests may be in flight:
        /// <code>
        /// lock (client.DefaultRequestHeaders)
        /// {
        ///     client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthSchemes.Bearer, accessToken);
        /// }
        /// </code>
        /// </example>
        public HttpRequestHeaders DefaultRequestHeaders { get; } = CreateRequestHeaders();

        /// <summary>
        /// Raises the <see cref="Disposing"/> event, then disposes the <see cref="HttpClient"/> of this client if the client owns it, or releases
        /// the reference of the client to a shared one.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Begins a scope that sets or removes request properties of the requests this client sends until the scope is disposed.
        /// </summary>
        /// <param name="properties">The properties to set, or, with a <see langword="null"/> value, to remove.</param>
        /// <returns>An <see cref="IDisposable"/> that ends the scope when it is disposed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="properties"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// Request properties carry values for message handlers and for the handlers of <see cref="BeforeSendingRequest"/>; they are not sent to the server.
        /// </para>
        /// <para>
        /// A scope flows with the asynchronous context of the code that begins it: it applies to the requests that code makes, including in methods it
        /// awaits, but not to requests made concurrently by unrelated code. Scopes can be nested, and an inner scope overrides an outer one.
        /// </para>
        /// </remarks>
        public virtual IDisposable BeginPropertyScope(IEnumerable<KeyValuePair<string, object?>> properties)
        {
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            return _scopedProperties.BeginScope(properties);
        }

        /// <summary>
        /// Begins a scope that sets or removes headers of the requests this client sends until the scope is disposed.
        /// </summary>
        /// <param name="headers">The headers to set, or, with a <see langword="null"/> value, to remove.</param>
        /// <returns>An <see cref="IDisposable"/> that ends the scope when it is disposed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="headers"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// The headers of the scope replace the headers of the same name in <see cref="DefaultRequestHeaders"/>. A scope flows with the asynchronous
        /// context of the code that begins it: it applies to the requests that code makes, including in methods it awaits, but not to requests made
        /// concurrently by unrelated code. Scopes can be nested, and an inner scope overrides an outer one.
        /// </para>
        /// <para>
        /// The <see cref="System.Net.Http.HttpClient.DefaultRequestHeaders"/> of the underlying <see cref="HttpClient"/> are added to a request only for
        /// the headers it does not already have, so a header that a scope removes is still sent if the <see cref="HttpClient"/> sets it. Keep the
        /// default headers of the <see cref="HttpClient"/> empty, and set them on <see cref="DefaultRequestHeaders"/> instead.
        /// </para>
        /// </remarks>
        public virtual IDisposable BeginHeaderScope(IEnumerable<KeyValuePair<string, string?>> headers)
        {
            if (headers is null)
                throw new ArgumentNullException(nameof(headers));

            return _scopedHeaders.BeginScope(headers);
        }

        /// <summary>
        /// Sends an asynchronous HTTP request with the specified method, URI, and payload, and returns the response body read as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content (optional).</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> or <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, or server certificate validation.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token, or if the timeout of the underlying <see cref="System.Net.Http.HttpClient"/> elapses.</exception>
        public virtual Task<T?> SendAsync<T>(HttpMethod method, string uri, HttpContent? payload = default, CancellationToken cancellationToken = default)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            return SendCoreAsync<T>(method, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends the request of <see cref="SendAsync{T}(HttpMethod, string, HttpContent?, CancellationToken)"/> after its arguments have been validated.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="cancellationToken">A token for canceling the request.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        private async Task<T?> SendCoreAsync<T>(HttpMethod method, string uri, HttpContent? payload, CancellationToken cancellationToken)
        {
            using var request = CreateHttpRequest(method, uri, typeof(T));
            request.Content = payload;

            using var response = await DispatchWithRetriesAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
            return (T?)await DeserializeContentAsync(response, typeof(T), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an asynchronous HTTP request with the specified method, URI, and payload, without processing the response body.
        /// </summary>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content (optional).</param>
        /// <param name="completionOption">
        /// When the operation completes: after the whole response body has been read (<see cref="HttpCompletionOption.ResponseContentRead"/>, the default),
        /// or as soon as the response headers have been read (<see cref="HttpCompletionOption.ResponseHeadersRead"/>).
        /// </param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> or <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, or server certificate validation.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token, or if the timeout of the underlying <see cref="System.Net.Http.HttpClient"/> elapses.</exception>
        /// <remarks>
        /// With <see cref="HttpCompletionOption.ResponseHeadersRead"/>, the response body is not buffered: it is read from the network as the caller
        /// reads the response content, and the caller must dispose the response to release the connection. <see cref="System.Net.Http.HttpClient.Timeout"/> then covers
        /// only the time until the headers arrive, and a failure while the body is read is not retried.
        /// </remarks>
        public virtual Task<HttpResponseMessage> SendAsync
        (
            HttpMethod method,
            string uri,
            HttpContent? payload = default,
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
            CancellationToken cancellationToken = default
        )
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            return SendCoreAsync(method, uri, payload, completionOption, cancellationToken);
        }

        /// <summary>
        /// Sends the request of <see cref="SendAsync(HttpMethod, string, HttpContent?, HttpCompletionOption, CancellationToken)"/> after its arguments
        /// have been validated.
        /// </summary>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="completionOption">When the operation completes.</param>
        /// <param name="cancellationToken">A token for canceling the request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
        private async Task<HttpResponseMessage> SendCoreAsync
        (
            HttpMethod method,
            string uri,
            HttpContent? payload,
            HttpCompletionOption completionOption,
            CancellationToken cancellationToken
        )
        {
            using var request = CreateHttpRequest(method, uri, responseObjectType: null);
            request.Content = payload;

            return await DispatchWithRetriesAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a request, and retries it after a transient connection failure, a timeout, or an error response that a handler retries.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="completionOption">When the operation completes: after the whole response body has been read, or as soon as the response headers have been read.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the <see cref="HttpResponseMessage"/> received in response to the request.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, or server certificate validation.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token, or if the timeout of the underlying <see cref="System.Net.Http.HttpClient"/> elapses.</exception>
        /// <remarks>
        /// After a transient connection failure or a timeout, the request is retried if <see cref="RetryPolicy"/> allows it. After an error response,
        /// it is retried if one of <see cref="ErrorHandlers"/> decides so. Otherwise, the failure is thrown.
        /// </remarks>
        /// <seealso cref="RetryPolicy"/>
        /// <seealso cref="ErrorHandlers"/>
        protected virtual Task<HttpResponseMessage> DispatchWithRetriesAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return DispatchWithRetriesCoreAsync(request, completionOption, cancellationToken);
        }

        /// <summary>
        /// Sends the request of <see cref="DispatchWithRetriesAsync"/> and retries it, after its arguments have been validated.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="completionOption">When the operation completes.</param>
        /// <param name="cancellationToken">A token for canceling the request.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the <see cref="HttpResponseMessage"/> received in response to the request.</returns>
        private async Task<HttpResponseMessage> DispatchWithRetriesCoreAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            using var cloneManager = new HttpRequestMessageCloneManager(request);
            var retryState = new HttpRetryState();
            for (; ; )
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await DispatchAsync(cloneManager.RequestToSend, completionOption, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpResponseException httpError) when (httpError.ResponseMessage is not null)
                {
                    var decision = await DecideOnRetryAsync(httpError, cloneManager.RequestToSend, httpError.ResponseMessage, retryState, cancellationToken).ConfigureAwait(false);
                    if (!cloneManager.TryApplyDecision(decision))
                        throw;
                }
                catch (HttpRequestException networkError) when (networkError.IsTransientNetworkError())
                {
                    var decision = await DecideOnRetryAsync(networkError, cloneManager.RequestToSend, retryState, cancellationToken).ConfigureAwait(false);
                    if (!cloneManager.TryApplyDecision(decision))
                        throw;
                }
                catch (TaskCanceledException timeoutError) when (!cancellationToken.IsCancellationRequested)
                {
                    var networkError = new HttpRequestException("The HTTP request timed out.", timeoutError);
                    var decision = await DecideOnRetryAsync(networkError, cloneManager.RequestToSend, retryState, cancellationToken).ConfigureAwait(false);
                    if (!cloneManager.TryApplyDecision(decision))
                        throw;
                }
            }
        }

        /// <summary>
        /// Sends a request once, without retries.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="completionOption">When the operation completes: after the whole response body has been read, or as soon as the response headers have been read.</param>
        /// <param name="cancellationToken">A token for canceling the request.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the <see cref="HttpResponseMessage"/> received in response to the request.</returns>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, or server certificate validation.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token, or if the timeout of the underlying <see cref="System.Net.Http.HttpClient"/> elapses.</exception>
        /// <remarks>
        /// This method returns the response if its status code indicates success; for an error status code, it throws an <see cref="HttpResponseException"/>.
        /// It raises <see cref="BeforeSendingRequest"/> before sending and <see cref="AfterReceivingResponse"/> when the response arrives.
        /// </remarks>
        protected virtual Task<HttpResponseMessage> DispatchAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            return DispatchCoreAsync(request, completionOption, cancellationToken);
        }

        /// <summary>
        /// Sends the request of <see cref="DispatchAsync"/> once, after its arguments have been validated.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="completionOption">When the operation completes.</param>
        /// <param name="cancellationToken">A token for canceling the request.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the <see cref="HttpResponseMessage"/> received in response to the request.</returns>
        private async Task<HttpResponseMessage> DispatchCoreAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            OnBeforeSendingRequest(request);
#if !NETSTANDARD2_0
            var response = await _httpClient.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
#else
            // HttpClient on .NET Framework disposes the request content after sending, but a retry sends the same content again.
            var content = request.Content;
            if (content is not null)
                request.Content = new NonOwningContent(content);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                request.Content = content;
            }
#endif
            try
            {
                response.RequestMessage = request;
                OnAfterReceivingResponse(response);

                if (response.IsSuccessStatusCode)
                    return response;

                throw await ToExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Decides whether to retry a request after a transient connection failure or a timeout.
        /// </summary>
        /// <param name="error">The <see cref="HttpRequestException"/> encapsulating details of the encountered error during the HTTP request execution.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that led to the failed response.</param>
        /// <param name="retryState">The retry state of the call, shared by all its attempts.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>, indicating whether to retry the request or that the error is unrecoverable.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="error"/>, <paramref name="request"/> or <paramref name="retryState"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The base implementation retries as <see cref="RetryPolicy"/> decides. An override that creates its own <see cref="HttpRequestErrorContext"/> passes <paramref name="retryState"/>
        /// to it, so that the retry sessions are kept across the attempts of the call.
        /// </remarks>
        /// <seealso cref="RetryPolicy"/>
        protected virtual Task<HttpErrorHandlerResult> DecideOnRetryAsync
        (
            HttpRequestException error,
            HttpRequestMessage request,
            HttpRetryState retryState,
            CancellationToken cancellationToken
        )
        {
            var ctx = new HttpRequestErrorContext(this, request, error, retryState);
            return ctx.ScheduleRetryAsync(this, RetryPolicy.CreateSession, cancellationToken);
        }

        /// <summary>
        /// Decides whether to retry a request after an error response.
        /// </summary>
        /// <param name="error">The <see cref="HttpResponseException"/> encapsulating details of the encountered error during the HTTP request execution.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that led to the failed response.</param>
        /// <param name="response">The received <see cref="HttpResponseMessage"/> indicating a failure.</param>
        /// <param name="retryState">The retry state of the call, shared by all its attempts.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>, indicating whether to retry the request or that the error is unrecoverable.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="error"/>, <paramref name="request"/>, <paramref name="response"/> or <paramref name="retryState"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The base implementation asks the handlers in <see cref="ErrorHandlers"/> that can handle the status code, in order, until one of them
        /// decides to retry. An override that
        /// creates its own <see cref="HttpResponseErrorContext"/> passes <paramref name="retryState"/> to it, so that the retry sessions are kept across the attempts
        /// of the call.
        /// </remarks>
        /// <seealso cref="ErrorHandlers"/>
        protected virtual Task<HttpErrorHandlerResult> DecideOnRetryAsync
        (
            HttpResponseException error,
            HttpRequestMessage request,
            HttpResponseMessage response,
            HttpRetryState retryState,
            CancellationToken cancellationToken
        )
        {
            var ctx = new HttpResponseErrorContext(this, request, response, error, retryState);
            return ConsultErrorHandlersAsync(ctx, cancellationToken);
        }

        /// <summary>
        /// Asks the error handlers for the status code of the response, in order, until one of them decides to retry.
        /// </summary>
        /// <param name="ctx">The context of the failed response.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to the decision of the first handler that retries, or <see cref="HttpErrorHandlerResult.NoRetry"/>.</returns>
        private async Task<HttpErrorHandlerResult> ConsultErrorHandlersAsync(HttpResponseErrorContext ctx, CancellationToken cancellationToken)
        {
            var response = ctx.Response;

            foreach (var errorHandler in ErrorHandlers.GetHandlersFor(response.StatusCode))
            {
                var decision = await errorHandler.DecideOnRetryAsync(ctx, cancellationToken).ConfigureAwait(false);
                if (decision.RequestToRetry is not null)
                {
                    decision.RequestToRetry.GetPropertyBag()[HttpRequestMessagePropertyKeys.ErrorHandler] = errorHandler;
                    return decision;
                }
            }

            return HttpErrorHandlerResult.NoRetry;
        }

        /// <summary>
        /// Creates the exception for an error response.
        /// </summary>
        /// <param name="response">The HTTP response message to convert.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an <see cref="HttpResponseException"/> object
        /// that represents the error extracted from the response.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If <see cref="ResponseErrorType"/> is set, this method reads the response content into that type and stores the result in
        /// <see cref="HttpResponseException.ResponseObject"/>. If the result implements <see cref="IHttpErrorResponse"/>, it creates the exception;
        /// otherwise, or if the content cannot be read, the exception carries the status code and a default message.
        /// </remarks>
        /// <seealso cref="ResponseErrorType"/>
        protected virtual Task<HttpResponseException> ToExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));

            return ToExceptionCoreAsync(response, cancellationToken);
        }

        /// <summary>
        /// Converts the response of <see cref="ToExceptionAsync"/> into an exception, after its arguments have been validated.
        /// </summary>
        /// <param name="response">The HTTP response message to convert.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the exception that represents the error.</returns>
        private async Task<HttpResponseException> ToExceptionCoreAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseObject = default(object);
            if (ResponseErrorType is not null && response.Content is not null && response.Content.Headers.ContentLength != 0)
            {
                try
                {
                    responseObject = await DeserializeContentAsync(response, ResponseErrorType, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpContentException)
                {
                    // Ignore errors
                }
            }

            var exception = responseObject is IHttpErrorResponse responseError
                ? responseError.ToException(response.StatusCode)
                : new HttpResponseException(response.StatusCode, $"Request failed with status code {(int)response.StatusCode} {response.ReasonPhrase}.");

            exception.ResponseMessage = response;
            exception.ResponseObject = responseObject;
            return exception;
        }

        /// <summary>
        /// Reads the body of a response into an object of the specified type.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to be read.</param>
        /// <param name="objectType">The type of object to which the response body is to be converted.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task representing the asynchronous operation, with the deserialized response body as an object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> or <paramref name="objectType"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpContentException">Thrown when the response body is empty, the content type is unsupported, or parsing the response fails.</exception>
        /// <remarks>
        /// This method reads the content with the first formatter in <see cref="ContentFormatters"/> that can read its media type into <paramref name="objectType"/>.
        /// If the formatter fails, the <see cref="HttpContentException"/> carries its exception as the inner exception.
        /// </remarks>
        /// <seealso cref="ContentFormatters"/>
        protected virtual Task<object?> DeserializeContentAsync(HttpResponseMessage response, Type objectType, CancellationToken cancellationToken)
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));
            if (objectType is null)
                throw new ArgumentNullException(nameof(objectType));

            return DeserializeContentCoreAsync(response, objectType, cancellationToken);
        }

        /// <summary>
        /// Deserializes the response body of <see cref="DeserializeContentAsync"/>, after its arguments have been validated.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to be read.</param>
        /// <param name="objectType">The type of object to which the response body is to be converted.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task representing the asynchronous operation, with the deserialized response body as an object.</returns>
        private async Task<object?> DeserializeContentCoreAsync(HttpResponseMessage response, Type objectType, CancellationToken cancellationToken)
        {
            if (response.Content is null || response.Content.Headers.ContentLength == 0)
                throw Error("The response body is empty.");

            var mediaType = (response.Content.Headers.ContentType?.MediaType)
                ?? throw Error("The media type of the response is unspecified.");

            var formatter = ContentFormatters.GetReaderFor(mediaType, objectType)
                ?? throw Error($"Unable to deserialize response body due to the absence of a content formatter that reads '{mediaType}' media type.");

            try
            {
                return await formatter.ReadAsync(response.Content, objectType, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                throw Error("Failed to deserialize response body due to a parsing error. See inner exception for details.", error);
            }

            HttpContentException Error(string message, Exception? innerException = null)
            {
                return new HttpContentException(message, innerException)
                {
                    Content = response.Content,
                    ObjectType = objectType,
                };
            }
        }

        /// <summary>
        /// Creates a request with the specified method and URI, and with the headers and properties of the client.
        /// </summary>
        /// <param name="method">The HTTP method of the request.</param>
        /// <param name="uri">The URI to which the request will be sent, absolute or relative to <see cref="BaseAddress"/>.</param>
        /// <param name="responseObjectType">The type of the object expected in the response, or <see langword="null"/> if the response body is not read.</param>
        /// <returns>The new <see cref="HttpRequestMessage"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> or <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// The request gets the headers of <see cref="DefaultRequestHeaders"/>, overridden by the headers of the active scopes. If neither sets an
        /// <c>Accept</c> header, it gets one with the media types that <see cref="ContentFormatters"/> can read into <paramref name="responseObjectType"/>
        /// and into <see cref="ResponseErrorType"/>, or <c>*/*</c> if <paramref name="responseObjectType"/> is <see langword="null"/>.
        /// </para>
        /// <para>
        /// The request gets the properties of the active scopes, and the following properties:
        /// <list type="bullet">
        ///   <item>
        ///     <term><see cref="HttpRequestMessagePropertyKeys.TransactionId"/></term>
        ///     <description>
        ///     A unique identifier (<see cref="Guid"/>) of the request, which the clones made for its retries keep, so that the attempts of a call can be correlated in logs.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="HttpRequestMessagePropertyKeys.ResponseObjectType"/></term>
        ///     <description>
        ///     The .NET type (<see cref="Type"/>) expected in the response, if any.
        ///     </description>
        ///   </item>
        /// </list>
        /// </para>
        /// </remarks>
        protected virtual HttpRequestMessage CreateHttpRequest(HttpMethod method, string uri, Type? responseObjectType)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            var requestUri = _baseAddress is null ? new Uri(uri) : new Uri(_baseAddress, uri);
            var request = new HttpRequestMessage(method, requestUri);

            AddRequestHeaders();
            AddRequestProperties();

            return request;

            void AddRequestHeaders()
            {
                lock (DefaultRequestHeaders)
                {
                    foreach (var header in DefaultRequestHeaders)
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (_scopedHeaders.HasActiveScope)
                {
                    foreach (var header in _scopedHeaders)
                    {
                        request.Headers.Remove(header.Key);
                        if (header.Value is not null)
                            request.Headers.Add(header.Key, header.Value);
                    }
                }

                if (!request.Headers.Contains(nameof(HttpRequestHeader.Accept)))
                {
                    foreach (var mediaType in ContentFormatters.GetAcceptableMediaTypes(responseObjectType, ResponseErrorType))
                        request.Headers.Accept.Add(MediaTypeHeaderValueStore.Get(mediaType));
                }
            }

            void AddRequestProperties()
            {
                var properties = request.GetPropertyBag();
                properties[HttpRequestMessagePropertyKeys.TransactionId] = Guid.NewGuid();
                properties[HttpRequestMessagePropertyKeys.ResponseObjectType] = responseObjectType;

                if (_scopedProperties.HasActiveScope)
                {
                    foreach (var property in _scopedProperties)
                    {
                        if (property.Value is not null)
                            properties[property.Key] = property.Value;
                        else
                            properties.Remove(property.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="Disposing"/> event and releases the <see cref="HttpClient"/> of this client, when called from <see cref="Dispose()"/>.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>; <see langword="false"/> when called from a finalizer.</param>
        /// <remarks>
        /// The <see cref="HttpRestClient"/> class has no finalizer, so this method is called with <paramref name="disposing"/> set to <see langword="false"/>
        /// only by a finalizer that a derived class declares. A derived class that owns unmanaged resources must declare its own finalizer that calls this
        /// method with <see langword="false"/>.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    OnDisposing();
                }
                finally
                {
                    _disposable?.Dispose();
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="BeforeSendingRequest"/> event.
        /// </summary>
        /// <param name="request">The HTTP request message that was created.</param>
        /// <remarks>
        /// This method is called before each attempt of a request is sent, including retries.
        /// </remarks>
        protected virtual void OnBeforeSendingRequest(HttpRequestMessage request)
        {
            BeforeSendingRequest?.Invoke(this, new HttpRequestMessageEventArgs(request));
        }

        /// <summary>
        /// Raises the <see cref="AfterReceivingResponse"/> event.
        /// </summary>
        /// <param name="response">The HTTP response message that was received.</param>
        /// <remarks>
        /// This method is called after a response is received and before the client processes it.
        /// </remarks>
        protected virtual void OnAfterReceivingResponse(HttpResponseMessage response)
        {
            AfterReceivingResponse?.Invoke(this, new HttpResponseMessageEventArgs(response));
        }

        /// <summary>
        /// Raises the <see cref="Disposing"/> event.
        /// </summary>
        /// <remarks>
        /// This method is called when the client is disposed, before it releases its <see cref="HttpClient"/>.
        /// </remarks>
        protected virtual void OnDisposing()
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}
