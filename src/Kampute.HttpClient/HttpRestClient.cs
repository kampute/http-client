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
    /// Facilitates HTTP communication with RESTful APIs by wrapping <see cref="HttpClient"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="HttpRestClient"/> abstracts the complexities of <see cref="HttpClient"/>, supporting the sharing of a single <see cref="HttpClient"/>
    /// instance across multiple <see cref="HttpRestClient"/> instances. This optimizes resource use and connection management, enhancing performance by
    /// reusing HTTP connections, especially during concurrent access to various services or API endpoints.
    /// </para>
    /// <para>
    /// The client allows for scoped request headers and properties, providing temporary configurations that do not alter global settings. This ensures
    /// that changes remain isolated to specific contexts, increasing maintainability and reducing configuration errors during runtime.
    /// </para>
    /// <para>
    /// It includes a <see cref="ContentFormatters"/> collection that converts between .NET objects and HTTP content: it reads response content into
    /// .NET objects based on the response's <c>Content-Type</c>, and writes the payloads of <see cref="HttpRestClientExtensions.SendObjectAsync{T}(HttpRestClient, HttpMethod, string, object, string, CancellationToken)"/>.
    /// If the <c>Accept</c> header is not predefined, the client dynamically adjusts it based on the configured formatters and the expected .NET object type.
    /// </para>
    /// <para>
    /// Transient failures and network interruptions are managed via the <see cref="RetryPolicy"/> property, which outlines retry logic and wait times
    /// between retries. This strategic approach helps avoid server overloads and improves communication success without excessive resource use.
    /// </para>
    /// <para>
    /// Extensible error handling is enabled through the <see cref="ErrorHandlers"/> collection, allowing custom <see cref="IHttpErrorHandler"/> implementations
    /// to handle specific HTTP errors with tailored strategies.
    /// </para>
    /// <para>
    /// Lifecycle events like <see cref="BeforeSendingRequest"/> and <see cref="AfterReceivingResponse"/> enhance request and response handling by enabling
    /// modifications, inspections, and logging, allowing for a highly customizable interaction.
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
        /// Initializes a new instance of the <see cref="HttpRestClient"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor initializes the <see cref="HttpRestClient"/> using a shared <see cref="HttpClient"/> instance acquired from the <see cref="SharedHttpClient"/> static class.
        /// </remarks>
        public HttpRestClient()
            : this(SharedHttpClient.AcquireReference())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRestClient"/> class with the specified shared <see cref="HttpClient"/> reference.
        /// </summary>
        /// <param name="httpClientReference">A reference to a shared <see cref="HttpClient"/> instance, managed as <see cref="SharedDisposable{T}.Reference"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpClientReference"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This constructor takes ownership of the shared <see cref="HttpClient"/> reference and ensures it is properly released when the <see cref="HttpRestClient"/> is disposed.
        /// </remarks>
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
        /// <param name="httpClient">The <see cref="HttpClient"/> to be used by the <see cref="HttpRestClient"/>.</param>
        /// <param name="disposeClient">Specifies whether the <see cref="HttpRestClient"/> should dispose of the provided <see cref="HttpClient"/> when the <see cref="HttpRestClient"/> is disposed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpClient"/> is <see langword="null"/>.</exception>
        public HttpRestClient(HttpClient httpClient, bool disposeClient = true)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (disposeClient)
                _disposable = httpClient;
        }

        /// <summary>
        /// Occurs when a new HTTP request message is about to be sent.
        /// </summary>
        /// <remarks>
        /// This event provides an opportunity for subscribers to modify the <see cref="HttpRequestMessage"/> before it is sent. Common modifications include adding
        /// custom headers, changing request properties, or logging request information. Modifications made to the request in this event are included in the outgoing
        /// HTTP request.
        /// </remarks>
        public event EventHandler<HttpRequestMessageEventArgs>? BeforeSendingRequest;

        /// <summary>
        /// Occurs when an HTTP response has been received.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This event is raised after an HTTP response is received but before the response is processed further. It provides a way for subscribers to inspect the
        /// <see cref="HttpResponseMessage"/>. This can be useful for logging response details, handling specific HTTP status codes, or modifying the response content
        /// or headers before they are processed by the rest of the application.
        /// </para>
        /// <para>
        /// For requests sent with <see cref="HttpCompletionOption.ResponseHeadersRead"/>, such as those of <see cref="HttpRestClientExtensions.GetAsStreamAsync"/>,
        /// <see cref="HttpRestClientExtensions.GetToStreamAsync"/> and <see cref="HttpRestClientExtensions.DownloadAsync"/>, the event is raised once the headers arrive
        /// and the body is not buffered. A subscriber that reads the response content consumes the body stream, which leaves nothing for the caller.
        /// </para>
        /// </remarks>
        public event EventHandler<HttpResponseMessageEventArgs>? AfterReceivingResponse;

        /// <summary>
        /// Occurs just before the <see cref="HttpRestClient"/> is disposed.
        /// </summary>
        /// <remarks>
        /// This event provides a way for subscribers to perform cleanup or other actions before the client is disposed.
        /// </remarks>
        public event EventHandler<EventArgs>? Disposing;

        /// <summary>
        /// Gets or sets the base address for HTTP requests.
        /// </summary>
        /// <value>
        /// The base address for HTTP requests.
        /// </value>
        /// <remarks>
        /// <para>
        /// If the provided base address does not end with a slash, one is automatically appended to ensure consistent URL resolution behavior.
        /// </para>
        /// <para>
        /// This is important because the presence or absence of a trailing slash affects how relative URIs are combined with the base address.
        /// For example, with a base address of <c>http://example.com/api</c> (no trailing slash), a relative URI of <c>users</c> resolves to
        /// <c>http://example.com/users</c>. Conversely, with <c>http://example.com/api/</c>, it resolves to <c>http://example.com/api/users</c>.
        /// </para>
        /// <para>
        /// By appending the slash when missing, the library ensures predictable and correct routing of requests.
        /// </para>
        /// <para>
        /// It's also worth noting that a <see langword="null"/> value for the base address is acceptable and indicates that no base address is set. In such cases,
        /// any HTTP request must use an absolute URL.
        /// </para>
        /// </remarks>
        public Uri? BaseAddress
        {
            get => _baseAddress;
            set => _baseAddress = value is null || value.AbsolutePath.EndsWith("/") ? value : new Uri(value, "/");
        }

        /// <summary>
        /// Gets or sets the retry policy for transient connection failures during HTTP requests.
        /// </summary>
        /// <value>
        /// The <see cref="IHttpRetryPolicy"/> that decides whether and when a request is retried after a transient connection failure.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property specifies the retry logic applied exclusively to connection failures, not to the processing of server responses. It determines
        /// if and when the client should retry a failed connection attempt before giving up. This approach is crucial for dealing with transient network
        /// issues or temporary server unavailability. The default is <see cref="HttpRetryPolicy.None"/>. To retry, assign a policy built from a retry strategy, such as
        /// <c>RetryStrategies.Exponential(TimeSpan.FromSeconds(1)).WithMaxAttempts(5).ToHttpRetryPolicy()</c>.
        /// </para>
        /// <para>
        /// The retry budget of this policy covers connection failures only. Each error handler that retries error responses keeps its own
        /// budget for the same request, so a request that fails in several ways can be retried more times in total than this policy allows.
        /// </para>
        /// </remarks>
        public IHttpRetryPolicy RetryPolicy
        {
            get => _retryPolicy;
            set => _retryPolicy = value ?? HttpRetryPolicy.None;
        }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> used to deserialize the response body when the response status code indicates an error.
        /// </summary>
        /// <value>
        /// The <see cref="Type"/> used to deserialize the response body when the response status code indicates an error.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property specifies the <see cref="Type"/> that the <see cref="HttpRestClient"/> will use to deserialize the response content in cases
        /// where the HTTP response indicates an error. It is important to ensure that the custom type specified is compatible with the expected error
        /// response format and can be read by the content formatters in <see cref="ContentFormatters"/>.
        /// </para>
        /// <para>
        /// When the specified type implements the <see cref="IHttpErrorResponse"/> interface, the deserialized object is utilized to construct a more
        /// informative exception. This mechanism enables the integration of custom error handling strategies by leveraging structured error information
        /// returned from the server.
        /// </para>
        /// </remarks>
        public Type? ResponseErrorType { get; set; }

        /// <summary>
        /// Gets the mutable collection of HTTP error handlers used for handling error responses.
        /// </summary>
        /// <value>
        /// The mutable collection of HTTP error handlers used for handling error responses.
        /// </value>
        /// <remarks>
        /// This property provides access to a collection of <see cref="IHttpErrorHandler"/> instances that are used to handle
        /// HTTP error responses. The handlers in this collection are tried in order to handle errors.
        /// </remarks>
        public HttpErrorHandlerCollection ErrorHandlers { get; } = [];

        /// <summary>
        /// Gets the mutable collection of content formatters that read response content and write request payloads.
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
        ///  Gets the headers which should be sent with each request.
        /// </summary>
        /// <value>
        /// The headers which should be sent with each request.
        /// </value>
        /// <remarks>
        /// <see cref="HttpHeaders"/> is not thread-safe. The client locks this collection while it copies the headers into each new request, and
        /// <see cref="Kampute.HttpClient.ErrorHandlers.HttpError401Handler"/> locks it while it updates the <c>Authorization</c> header. Code that
        /// changes this collection while requests are in flight must lock the same object, for example <c>lock (client.DefaultRequestHeaders) { ... }</c>.
        /// </remarks>
        public HttpRequestHeaders DefaultRequestHeaders { get; } = CreateRequestHeaders();

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="HttpRestClient"/> and optionally disposes of the managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Begins a new scope with the specified request properties.
        /// </summary>
        /// <param name="properties">The request properties to be applied exclusively during the lifetime of the new scope.</param>
        /// <returns>An <see cref="IDisposable"/> representing the new scope. Disposing of this object will end the scope and revert changes in the request properties.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="properties"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// This method creates a scope associated with the current <see cref="HttpRestClient"/> instance to add, modify or remove any request properties in subsequent
        /// requests during the lifetime of this scope. To remove a property, use <see langword="null"/> for its value.
        /// </para>
        /// <para>
        /// Upon disposing of the scope, all property adjustments are reverted, restoring the properties to their state before the scope was activated.
        /// </para>
        /// </remarks>
        public virtual IDisposable BeginPropertyScope(IEnumerable<KeyValuePair<string, object?>> properties)
        {
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            return _scopedProperties.BeginScope(properties);
        }

        /// <summary>
        /// Begins a new scope with the specified request headers.
        /// </summary>
        /// <param name="headers">The request headers to be applied exclusively during the lifetime of the new scope.</param>
        /// <returns>An <see cref="IDisposable"/> representing the new scope. Disposing of this object will end the scope and revert changes in the request headers.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="headers"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// This method creates a scope associated with the current <see cref="HttpRestClient"/> instance to add, modify or remove any request header in subsequent
        /// requests during the lifetime of this scope. To remove a header, use <see langword="null"/> for its value.
        /// </para>
        /// <para>
        /// Any header modifications made within this scope take precedence over the client's default headers. Header adjustments by other active scopes are overridden
        /// by those provided in this scope. However, the default request headers set on the underlying <see cref="HttpClient"/> instance take precedence over the default
        /// and scoped headers of the <see cref="HttpRestClient"/> instance because they are applied later in the message handler pipeline. To avoid conflicts, it is
        /// recommended to keep the default request headers of the underlying <see cref="HttpClient"/> instance empty.
        /// </para>
        /// <para>
        /// Upon disposing of the scope, all header adjustments are reverted, restoring the headers to their state before the scope was activated.
        /// </para>
        /// </remarks>
        public virtual IDisposable BeginHeaderScope(IEnumerable<KeyValuePair<string, string?>> headers)
        {
            if (headers is null)
                throw new ArgumentNullException(nameof(headers));

            return _scopedHeaders.BeginScope(headers);
        }

        /// <summary>
        /// Sends an asynchronous HTTP request with the specified method, URI, and payload, returning response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content (optional).</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> or <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
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
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
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
        /// Sends an asynchronous HTTP request, with the possibility of retrying the request based on specific failure conditions.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="completionOption">When the operation completes: after the whole response body has been read, or as soon as the response headers have been read.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the <see cref="HttpResponseMessage"/> received in response to the request.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// This method is responsible for sending the HTTP request and optionally retrying it under specific failure conditions. The decision to retry a request is based
        /// on the nature of the failure, with potential consultation of external retry logic mechanisms.
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
        /// Asynchronously dispatches an HTTP request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="completionOption">When the operation completes: after the whole response body has been read, or as soon as the response headers have been read.</param>
        /// <param name="cancellationToken">A token for canceling the request.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the <see cref="HttpResponseMessage"/> received in response to the request.</returns>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// This method sends the provided HTTP request and returns the response if the status code indicates a success. For any error status codes, it fails fast by throwing
        /// an exception specific to the nature of the error. Additionally, the method incorporates pre-send and post-receive hooks for adding custom logic, such as modifying
        /// request headers or logging response details.
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
        /// Asynchronously evaluates transient network issues to decide the appropriate action based on the error context and predefined error
        /// handling strategies.
        /// </summary>
        /// <param name="error">The <see cref="HttpRequestException"/> encapsulating details of the encountered error during the HTTP request execution.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that led to the failed response.</param>
        /// <param name="retryState">The retry budgets of the call, shared by all its attempts.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>, indicating whether to retry the request or that the error is unrecoverable.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="error"/>, <paramref name="request"/> or <paramref name="retryState"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method assesses transient network issues, using the retry policy specified by <see cref="RetryPolicy"/>. It returns an
        /// <see cref="HttpErrorHandlerResult"/> that guides the next steps, either to retry the request with potentially modified parameters or
        /// to handle the error as unrecoverable. An override that creates its own <see cref="HttpRequestErrorContext"/> passes <paramref name="retryState"/>
        /// to it, so that the retry budgets are kept across the attempts of the call.
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
        /// Asynchronously evaluates failed HTTP responses to decide the appropriate action based on the error context and predefined error
        /// handling strategies.
        /// </summary>
        /// <param name="error">The <see cref="HttpResponseException"/> encapsulating details of the encountered error during the HTTP request execution.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that led to the failed response.</param>
        /// <param name="response">The received <see cref="HttpResponseMessage"/> indicating a failure.</param>
        /// <param name="retryState">The retry budgets of the call, shared by all its attempts.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>, indicating whether to retry the request or that the error is unrecoverable.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="error"/>, <paramref name="request"/>, <paramref name="response"/> or <paramref name="retryState"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method assesses HTTP request failures, leveraging error handling strategies within <see cref="ErrorHandlers"/>. It returns an <see cref="HttpErrorHandlerResult"/>
        /// that guides the next steps, either to retry the request with potentially modified parameters or to handle the error as unrecoverable. An override that
        /// creates its own <see cref="HttpResponseErrorContext"/> passes <paramref name="retryState"/> to it, so that the retry budgets are kept across the attempts
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
        /// Converts an <see cref="HttpResponseMessage"/> into an appropriate exception.
        /// </summary>
        /// <param name="response">The HTTP response message to convert.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an <see cref="HttpResponseException"/> object
        /// that represents the error extracted from the response.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method endeavors to deserialize the response content into a <see cref="ResponseErrorType"/>, provided that the type is
        /// specified and implements the <see cref="IHttpErrorResponse"/> interface. Upon successful deserialization, the resulting data
        /// is transformed into an exception. If deserialization fails, a generic <see cref="HttpResponseException"/> is generated, incorporating
        /// the response's status code and a default error message.
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
        /// Asynchronously deserializes the body of an <see cref="HttpResponseMessage"/> and converts it into an object of a specified type.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to be read.</param>
        /// <param name="objectType">The type of object to which the response body is to be converted.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task representing the asynchronous operation, with the deserialized response body as an object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> or <paramref name="objectType"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpContentException">Thrown when the response body is empty, the content type is unsupported, or parsing the response fails.</exception>
        /// <remarks>
        /// This method reads the content with the first formatter in <see cref="ContentFormatters"/> that can read its media type into <paramref name="objectType"/>.
        /// In case of deserialization failures, an <see cref="HttpContentException"/> is thrown, which may contain an inner exception providing more details about
        /// the parsing error.
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
        /// Creates an <see cref="HttpRequestMessage"/> with the specified method and URI.
        /// </summary>
        /// <param name="method">The HTTP method to be used for the request, such as GET, POST, PUT, etc.</param>
        /// <param name="uri">The URI to which the request will be sent. Should be a valid, fully qualified URL.</param>
        /// <param name="responseObjectType">The type of the object expected to be contained in the response.</param>
        /// <returns>An <see cref="HttpRequestMessage"/> configured with the specified method and URI.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> or <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// This method constructs a new HTTP request message by setting the HTTP method and URI. It prepares the request for transmission by
        /// configuring both the headers and custom properties appropriate for the given context and operation.
        /// </para>
        /// <para>
        /// Headers are added or adjusted from both the default headers provided by the <see cref="DefaultRequestHeaders"/> property and any scoped
        /// headers that are active at the time of this request’s creation. Scoped headers are prioritized over default headers in case of key conflicts
        /// to ensure that context-specific modifications are respected.
        /// </para>
        /// <para>
        /// If an <c>Accept</c> header is absent in both default and scoped headers, it is added based on the media types that the content formatters can read
        /// into the specified <paramref name="responseObjectType"/>. If <paramref name="responseObjectType"/> is <see langword="null"/>, the header defaults to accepting all
        /// media types ("*/*").
        /// </para>
        /// <para>
        /// This method also includes scoped properties in the HTTP request message to provide additional context and facilitate easier tracking and processing
        /// of the request. In addition to the scoped properties, the following properties are added:
        /// <list type="bullet">
        ///   <item>
        ///     <term><see cref="HttpRequestMessagePropertyKeys.TransactionId"/></term>
        ///     <description>
        ///     A unique identifier (<see cref="Guid"/>) generated and assigned to each request, aiding in the request's tracking, debugging, and logging
        ///     processes. The unique identifier ensures that each request can be individually tracked, even when multiple requests are executed simultaneously
        ///     or when requests are retried due to transient failures.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="HttpRequestMessagePropertyKeys.ResponseObjectType"/></term>
        ///     <description>
        ///     Defines the .NET type (<see cref="Type"/>) expected in the response, if any. This metadata provides context that can improve debugging, enhance logging details,
        ///     and support error recovery strategies.
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
        /// Disposes the <see cref="HttpRestClient"/> instance.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from a <see cref="IDisposable.Dispose()"/> method.</param>
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
        /// This method is called to trigger the <see cref="BeforeSendingRequest"/> event. This allows for centralized
        /// handling of request modifications across various methods that send HTTP requests. The method is invoked
        /// before an <see cref="HttpRequestMessage"/> is sent.
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
        /// This method is called to trigger the <see cref="AfterReceivingResponse"/> event. This allows to react to the
        /// reception of an HTTP response. The method is invoked after a response is received from an HTTP request but
        /// before any processing is performed on the response.
        /// </remarks>
        protected virtual void OnAfterReceivingResponse(HttpResponseMessage response)
        {
            AfterReceivingResponse?.Invoke(this, new HttpResponseMessageEventArgs(response));
        }

        /// <summary>
        /// Raises the <see cref="Disposing"/> event.
        /// </summary>
        /// <remarks>
        /// This method is called as part of the disposal process of the <see cref="HttpRestClient"/> instance, specifically
        /// just before the client starts releasing its resources. It triggers the <see cref="Disposing"/> event, allowing
        /// subscribed entities to perform any necessary cleanup actions before the client is fully disposed.
        /// </remarks>
        protected virtual void OnDisposing()
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}
