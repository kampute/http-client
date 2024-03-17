// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.IO;
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
    /// <see cref="HttpRestClient"/> simplifies interactions with RESTful APIs by abstracting the complexities of <see cref="HttpClient"/>. It supports the 
    /// sharing of a single <see cref="HttpClient"/> instance across multiple <see cref="HttpRestClient"/> instances, optimizing resource use and connection 
    /// management. This shared approach enhances performance, especially when concurrently accessing various services or API endpoints, by reusing HTTP 
    /// connections.
    /// </para>
    /// <para>
    /// It features a <see cref="ResponseDeserializers"/> collection for automatic HTTP response content deserialization into .NET objects. The client selects 
    /// the appropriate deserializer for the response's <c>Content-Type</c>, easing the integration with API responses.
    /// </para>
    /// <para>
    /// Backoff strategies are implemented through the <see cref="BackoffStrategy"/> property, providing a mechanism for handling transient failures and network 
    /// interruptions. These strategies dictate the logic for retrying requests after failures, including how long to wait between retries. By employing a
    /// proper backoff strategy, the client can avoid overwhelming the server or network, improving the chances of successful communication without compromising 
    /// resource utilization.
    /// </para>
    /// <para>
    /// Extensible error handling is achieved through the <see cref="ErrorHandlers"/> collection. Custom <see cref="IHttpErrorHandler"/> implementations can 
    /// be employed to address specific HTTP errors, enabling tailored retry strategies and failure responses.
    /// </para>
    /// <para>
    /// The client enriches HTTP request and response handling with life-cycle events such as <see cref="BeforeSendingRequest"/> and <see cref="AfterReceivingResponse"/>. 
    /// These events allow for request modification, response inspection, and logging, facilitating a high degree of interaction customization.
    /// </para>
    /// <para>
    /// Through its design, <see cref="HttpRestClient"/> aims to offer a balance between ease of use and flexibility, making it a suitable choice for developers 
    /// looking to interact with RESTful APIs in a .NET environment. Whether for simple API consumption or complex, high-load scenarios, it provides the tools 
    /// necessary to create efficient, reliable, and customizable HTTP communication solutions.
    /// </para>
    /// </remarks>
    public class HttpRestClient : IDisposable, ICloneable
    {
        private static readonly MediaTypeWithQualityHeaderValue AnyMediaType = new("*/*", 0.1);

        private static readonly SharedDisposableManager<HttpClient> _sharedHttpClient = new(() =>
        {
            var messageHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            return new HttpClient(messageHandler, disposeHandler: true);
        });

        private readonly HttpClient _httpClient;
        private readonly bool _disposeClient;

        private Uri? _baseAddress = null;
        private Type? _responseErrorType = null;
        private IRetrySchedulerFactory _backoffStrategy = BackoffStrategies.None;
        private readonly HttpErrorHandlerCollection _errorHandlers = [];
        private readonly HttpContentDeserializerCollection _deserializers = [];
        private readonly HttpRequestHeaders _defaultRequestHeaders = CreateRequestHeaders();

        private static HttpRequestHeaders CreateRequestHeaders()
        {
            using var request = new HttpRequestMessage();
            return request.Headers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRestClient"/> class with a default <see cref="HttpClient"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor initializes the <see cref="HttpRestClient"/> with an internally shared <see cref="HttpClient"/> instance. 
        /// The <see cref="HttpClient"/> is configured with an <see cref="HttpClientHandler"/> that enables automatic decompression of 
        /// GZip and Deflate encoded content.
        /// </para>
        /// <para>
        /// This default constructor is ideal for scenarios requiring a straightforward and ready-to-use REST client without the need 
        /// for extensive customization of the underlying HTTP client. It provides a convenient option for quick setup and immediate 
        /// use, particularly useful for standard API consumption where default configurations suffice.
        /// </para>
        /// </remarks>
        public HttpRestClient()
            : this(_sharedHttpClient.Acquire(), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRestClient"/> class with the specified <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> to be used by the <see cref="HttpRestClient"/>.</param>
        /// <param name="disposeClient">Specifies whether the <see cref="HttpRestClient"/> should dispose of the provided <see cref="HttpClient"/> when the <see cref="HttpRestClient"/> is disposed.</param>
        /// <remarks>
        /// <para>
        /// This constructor allows the integration of a custom or shared <see cref="HttpClient"/> instance. Utilizing a shared <see cref="HttpClient"/> can provide several 
        /// benefits, such as improved resource utilization and socket management, especially in high-load scenarios. Shared instances help in avoiding socket exhaustion and 
        /// can reduce overhead by reusing existing connections where possible.
        /// </para>
        /// <para>
        /// When <paramref name="disposeClient"/> is set to <c>true</c>, the <see cref="HttpRestClient"/> takes responsibility for disposing of the <see cref="HttpClient"/>. This is suitable 
        /// when the <see cref="HttpClient"/> is used exclusively by the <see cref="HttpRestClient"/>. However, if the <see cref="HttpClient"/> is shared across different parts of the 
        /// application or by multiple instances of <see cref="HttpRestClient"/>, it is recommended to set <paramref name="disposeClient"/> to <c>false</c>. This ensures that disposing of 
        /// one <see cref="HttpRestClient"/> instance does not inadvertently impact other components using the same <see cref="HttpClient"/>.
        /// </para>
        /// <para>
        /// This constructor is particularly advantageous in scenarios requiring a <see cref="HttpClient"/> with specific configurations, such as custom message handlers, specialized 
        /// timeout settings, or advanced authentication mechanisms. It provides the necessary flexibility to align the <see cref="HttpRestClient"/> with existing infrastructure and 
        /// organizational policies.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpClient"/> is <c>null</c>.</exception>
        public HttpRestClient(HttpClient httpClient, bool disposeClient = true)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeClient = disposeClient;
        }

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        ~HttpRestClient() => Dispose(false);

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
        /// This event is raised after an HTTP response is received but before the response is processed further. It provides a way for subscribers to inspect the 
        /// <see cref="HttpResponseMessage"/>. This can be useful for logging response details, handling specific HTTP status codes, or modifying the response content 
        /// or headers before they are processed by the rest of the application.
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
        /// If the provided base address does not end with a slash, one is automatically appended. The presence or absence of this trailing 
        /// slash is significant in how relative URLs are resolved. For instance, if the base address is <see href="http://example.com/api"/> (no trailing 
        /// slash) and the relative URL is "users", the resolved URL will be <see href="http://example.com/users"/>. Conversely, if the base address ends 
        /// with a slash, like <see href="http://example.com/api/"/>, the resolved URL will be <see href="http://example.com/api/users"/>. This subtle 
        /// difference can be crucial in ensuring requests are routed correctly.
        /// </para>
        /// <para>
        /// It's also worth noting that a <c>null</c> value for the base address is acceptable and indicates that no base address is set. In such cases, 
        /// any HTTP request must use an absolute URL.
        /// </para>
        /// </remarks>
        public Uri? BaseAddress
        {
            get => _baseAddress;
            set => _baseAddress = value is null || value.AbsolutePath.EndsWith("/") ? value : new Uri(value, "/");
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
        /// response format and can be deserialized by the content deserializers available to the <see cref="HttpRestClient"/>.
        /// </para>
        /// <para>
        /// When the specified type implements the <see cref="IHttpErrorResponse"/> interface, the deserialized object is utilized to construct a more
        /// informative exception. This mechanism enables the integration of custom error handling strategies by leveraging structured error information
        /// returned from the server.
        /// </para>
        /// </remarks>
        public Type? ResponseErrorType
        {
            get => _responseErrorType;
            set => _responseErrorType = value;
        }

        /// <summary>
        /// Gets or sets the backoff strategy for handling transient connection failures during HTTP requests.
        /// </summary>
        /// <value>
        /// The backoff strategy for handling transient connection failures during HTTP requests.
        /// </value>
        /// <remarks>
        /// This property specifies the retry logic applied exclusively to connection failures, not to the processing of server responses. It determines 
        /// if and when the client should retry a failed connection attempt before giving up. This approach is crucial for dealing with transient network 
        /// issues or temporary server unavailability. The default is <see cref="BackoffStrategies.None"/>.
        /// </remarks>
        public IRetrySchedulerFactory BackoffStrategy
        {
            get => _backoffStrategy;
            set => _backoffStrategy = value ?? BackoffStrategies.None;
        }

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
        public HttpErrorHandlerCollection ErrorHandlers => _errorHandlers;

        /// <summary>
        /// Gets the mutable collection of HTTP content deserializers used for deserializing response content.
        /// </summary>
        /// <value>
        /// The mutable collection of HTTP content deserializers used for deserializing response content.
        /// </value>
        /// <remarks>
        /// This property provides access to a collection of <see cref="IHttpContentDeserializer"/> instances that are used to 
        /// deserialize the content of HTTP responses. The deserializers in this list are tried in order to deserialize the response 
        /// content into .NET objects.
        /// </remarks>
        public HttpContentDeserializerCollection ResponseDeserializers => _deserializers;

        /// <summary>
        ///  Gets the headers which should be sent with each request.
        /// </summary>
        /// <value>
        /// The headers which should be sent with each request.
        /// </value>
        public HttpRequestHeaders DefaultRequestHeaders => _defaultRequestHeaders;

        /// <summary>
        /// Creates a copy of the current <see cref="HttpRestClient"/> instance.
        /// </summary>
        /// <returns>A new <see cref="HttpRestClient"/> instance that is a copy of the current instance.</returns>
        /// <remarks>
        /// This method creates a new instance of <see cref="HttpRestClient"/> by copying the current instance's configuration, 
        /// and reuses the underlying <see cref="HttpClient"/> without taking ownership. This means that disposing of the cloned 
        /// instance will not dispose of the <see cref="HttpClient"/>.
        /// </remarks>
        public object Clone()
        {
            var clone = new HttpRestClient(_sharedHttpClient.Is(_httpClient) ? _sharedHttpClient.Acquire() : _httpClient, false)
            {
                _baseAddress = _baseAddress,
                _responseErrorType = _responseErrorType,
                _backoffStrategy = _backoffStrategy,
            };

            foreach (var errorHandler in _errorHandlers)
                clone._errorHandlers.Add(errorHandler);
            foreach (var deserializer in _deserializers)
                clone._deserializers.Add(deserializer);
            foreach (var header in _defaultRequestHeaders)
                clone._defaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="HttpRestClient"/> and optionally disposes of the managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Retrieves data from the specified URI and writes it to the provided stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream to write the downloaded data to. It must be writable.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload (optional).</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>
        /// A task that represents the asynchronous download operation. The task result contains the headers of the downloaded content. 
        /// If the response contains no content, null is returned.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/>, <paramref name="method"/>, or <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public async Task<HttpContentHeaders?> FetchToStreamAsync
        (
            Stream stream,
            HttpMethod method,
            string uri,
            HttpContent? payload = null,
            CancellationToken cancellationToken = default
        )
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            using var request = CreateHttpRequest(method, uri, null);
            request.Content = payload;

            using var response = await DispatchWithRetriesAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.Content is null)
                return null;

            await response.Content.CopyToAsync(stream).ConfigureAwait(false);
            return response.Content.Headers;
        }

        /// <summary>
        /// Sends an asynchronous HTTP request with the specified method, URI, and payload, and returns the response body deserialized as the specified type. 
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload (optional).</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>
        /// A task that represents the asynchronous operation, with a result of the specified type. If the response contains no content, the default value 
        /// for the type <typeparamref name="T"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> or <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public async Task<T?> SendAsync<T>(HttpMethod method, string uri, HttpContent? payload = null, CancellationToken cancellationToken = default)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            using var request = CreateHttpRequest(method, uri, typeof(T));
            request.Content = payload;

            using var response = await DispatchWithRetriesAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await DeserializeContentAsync(response.Content, typeof(T), cancellationToken).ConfigureAwait(false);
            return content is T value ? value : default;
        }

        /// <summary>
        /// Sends an asynchronous HTTP request with the specified method, URI, and payload, without processing the response body. 
        /// </summary>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload (optional).</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the headers of the response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> or <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public async Task<HttpResponseHeaders> SendAsync(HttpMethod method, string uri, HttpContent? payload = null, CancellationToken cancellationToken = default)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            using var request = CreateHttpRequest(method, uri, null);
            request.Content = payload;

            using var response = await DispatchWithRetriesAsync(request, cancellationToken).ConfigureAwait(false);
            return response.Headers;
        }

        /// <summary>
        /// Asynchronously dispatches an HTTP request, with the possibility of retrying the request based on specific failure conditions.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="cancellationToken">A token for canceling the request.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the <see cref="HttpResponseMessage"/> received in response to the request.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// This method is responsible for sending the HTTP request and optionally retrying it under specific failure conditions. The decision to retry a request is based 
        /// on the nature of the failure, with potential consultation of external retry logic mechanisms.
        /// </remarks>
        /// <seealso cref="BackoffStrategy"/>
        /// <seealso cref="ErrorHandlers"/>
        protected virtual async Task<HttpResponseMessage> DispatchWithRetriesAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            using var cloneManager = new HttpRequestMessageCloneManager(request);
            for (; ; )
            {
                try
                {
                    return await DispatchAsync(cloneManager.RequestToSend, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpResponseException httpError) when (httpError.ResponseMessage is not null)
                {
                    var decision = await DecideOnRetryAsync(httpError, cloneManager.RequestToSend, httpError.ResponseMessage, cancellationToken).ConfigureAwait(false);
                    if (!cloneManager.TryApplyDecision(decision))
                        throw;
                }
                catch (HttpRequestException networkError) when (networkError.IsTransientNetworkError())
                {
                    var decision = await BackoffAndDecideOnRetryAsync(networkError, cloneManager.RequestToSend, cancellationToken).ConfigureAwait(false);
                    if (!cloneManager.TryApplyDecision(decision))
                        throw;
                }
            }
        }

        /// <summary>
        /// Asynchronously dispatches an HTTP request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to send.</param>
        /// <param name="cancellationToken">A token for canceling the request.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the <see cref="HttpResponseMessage"/> received in response to the request.</returns>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// This method sends the provided HTTP request and returns the response if the status code indicates a success. For any error status codes, it fails fast by throwing 
        /// an exception specific to the nature of the error. Additionally, the method incorporates pre-send and post-receive hooks for adding custom logic, such as modifying 
        /// request headers or logging response details.
        /// </remarks>
        protected virtual async Task<HttpResponseMessage> DispatchAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            OnBeforeSendingRequest(request);
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
        /// Asynchronously evaluates failed HTTP responses to decide the appropriate action based on the error context and predefined error 
        /// handling strategies. 
        /// </summary>
        /// <param name="error">The <see cref="HttpResponseException"/> encapsulating details of the encountered error during the HTTP request execution.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that led to the failed response.</param>
        /// <param name="response">The received <see cref="HttpResponseMessage"/> indicating a failure.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>, indicating whether to retry the request or that the error is unrecoverable.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="error"/>, <paramref name="request"/> or <paramref name="response"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method assesses HTTP request failures, leveraging error handling strategies within <see cref="ErrorHandlers"/>. It returns an <see cref="HttpErrorHandlerResult"/> 
        /// that guides the next steps, either to retry the request with potentially modified parameters or to handle the error as unrecoverable.
        /// </remarks>
        /// <seealso cref="ErrorHandlers"/>
        protected virtual async Task<HttpErrorHandlerResult> DecideOnRetryAsync
        (
            HttpResponseException error,
            HttpRequestMessage request,
            HttpResponseMessage response,
            CancellationToken cancellationToken
        )
        {
            if (error is null)
                throw new ArgumentNullException(nameof(error));
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (response is null)
                throw new ArgumentNullException(nameof(response));

            var context = new HttpResponseErrorContext(this, request, response, error);

            foreach (var errorHandler in _errorHandlers.For(response.StatusCode))
            {
                var decision = await errorHandler.DecideOnRetryAsync(context, cancellationToken).ConfigureAwait(false);
                if (decision.RequestToRetry is not null)
                    return decision;
            }

            return HttpErrorHandlerResult.NoRetry;
        }

        /// <summary>
        /// Asynchronously evaluates transient network issues to decide the appropriate action based on the error context and predefined error 
        /// handling strategies. 
        /// </summary>
        /// <param name="error">The <see cref="HttpRequestException"/> encapsulating details of the encountered error during the HTTP request execution.</param>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that led to the failed response.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to an <see cref="HttpErrorHandlerResult"/>, indicating whether to retry the request or that the error is unrecoverable.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="error"/> or <paramref name="request"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method assesses transient network issues, leveraging backoff strategies specified by <see cref="BackoffStrategy"/>. It returns an 
        /// <see cref="HttpErrorHandlerResult"/> that guides the next steps, either to retry the request with potentially modified parameters or 
        /// to handle the error as unrecoverable.
        /// </remarks>
        /// <seealso cref="BackoffStrategy"/>
        protected virtual async Task<HttpErrorHandlerResult> BackoffAndDecideOnRetryAsync
        (
            HttpRequestException error,
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (error is null)
                throw new ArgumentNullException(nameof(error));
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (!request.CanClone())
                return HttpErrorHandlerResult.NoRetry;

            var scheduler = request.Properties.GetOrAdd(HttpRequestMessagePropertyKeys.RetryScheduler, _ =>
            {
                var ctx = new HttpRequestErrorContext(this, request, error);
                return _backoffStrategy.CreateScheduler(ctx);
            });

            if (await scheduler.WaitAsync(cancellationToken).ConfigureAwait(false))
                return HttpErrorHandlerResult.Retry(request.Clone());

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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method endeavors to deserialize the response content into a <see cref="ResponseErrorType"/>, provided that the type is 
        /// specified and implements the <see cref="IHttpErrorResponse"/> interface. Upon successful deserialization, the resulting data 
        /// is transformed into an exception. If deserialization fails, a generic <see cref="HttpResponseException"/> is generated, incorporating 
        /// the response's status code and a default error message.
        /// </remarks>
        /// <seealso cref="ResponseErrorType"/>
        protected async Task<HttpResponseException> ToExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));

            var responseObject = default(object);
            if (_responseErrorType is not null)
            {
                try
                {
                    responseObject = await DeserializeContentAsync(response.Content, _responseErrorType, cancellationToken).ConfigureAwait(false);
                }
                catch
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
        /// Asynchronously deserializes the content of an <see cref="HttpContent"/> object and converts it into an object of a specified type.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to be read.</param>
        /// <param name="objectType">The type of object to which the content is to be converted.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task representing the asynchronous operation, with the deserialized content as an object. Returns null if there is no content.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="objectType"/> is <c>null</c>.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type is unknown, not supported, or if there is an error parsing the response body.</exception>
        /// <remarks>
        /// This method uses configured content deserializers for content deserialization and supports custom content types. In case of deserialization 
        /// failures, an <see cref="HttpContentException"/> is thrown, which may contain an inner exception providing more details about the parsing error. 
        /// </remarks>        
        /// <seealso cref="ResponseDeserializers"/>
        protected async Task<object?> DeserializeContentAsync(HttpContent content, Type objectType, CancellationToken cancellationToken)
        {
            if (objectType is null)
                throw new ArgumentNullException(nameof(objectType));

            if (content is null || content.Headers.ContentLength == 0)
                return null;

            if (content.Headers.ContentType is null)
            {
                throw new HttpContentException("The content type of the response could not be determined.")
                {
                    Content = content
                };
            }

            var deserializerError = default(Exception);
            foreach (var deserializer in _deserializers.For(content.Headers.ContentType.MediaType, objectType))
            {
                try
                {
                    return await deserializer.DeserializeAsync(content, objectType, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    deserializerError = error;
                }
            }

            throw new HttpContentException($"The content type '{content.Headers.ContentType.MediaType}' could not be converted into type '{objectType.Name}'.", deserializerError)
            {
                Content = content
            };
        }

        /// <summary>
        /// Creates an <see cref="HttpRequestMessage"/> with the specified method and URI.
        /// </summary>
        /// <param name="method">The HTTP method to be used for the request, such as GET, POST, PUT, etc.</param>
        /// <param name="uri">The URI to which the request will be sent. Should be a valid, fully qualified URL.</param>
        /// <param name="responseObjectType">The type of the object expected to be contained in the response.</param>
        /// <returns>An <see cref="HttpRequestMessage"/> configured with the specified method and URI.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/> or <paramref name="uri"/> is <c>null</c>.</exception>
        /// <remarks>
        /// <para>
        /// This method constructs a new HTTP request message. It sets the method, URI, and headers based on the provided parameters. 
        /// The <c>Accept</c> headers are configured to align with the media types supported by the content deserializers for the specified 
        /// <paramref name="responseObjectType"/>. If <paramref name="responseObjectType"/> is <c>null</c>, all media types will be accepted.
        /// </para>
        /// <para>
        /// In addition, this method includes custom properties in the <see cref="HttpRequestMessage"/> to provide additional context and facilitate
        /// easier tracking and processing of the request. These properties are detailed below:
        /// <list type="bullet">
        /// <item>
        /// <term><see cref="HttpRequestMessagePropertyKeys.TransactionId"/></term>
        /// <description>
        /// A unique identifier (<see cref="Guid"/>) generated and assigned to each request, aiding in the request's tracking, debugging, and logging
        /// processes. The unique identifier ensures that each request can be individually tracked, even when multiple requests are executed simultaneously
        /// or when requests are retried due to transient failures.
        /// </description>
        /// </item>
        /// <item>
        /// <term><see cref="HttpRequestMessagePropertyKeys.ResponseObjectType"/></term>
        /// <description>
        /// Defines the .NET type expected in the response, if any. This metadata provides context that can improve debugging, enhance logging details,
        /// and support error recovery strategies.
        /// </description>
        /// </item>
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

            foreach (var header in _defaultRequestHeaders)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            foreach (var mediaType in _deserializers.GetSupportedMediaTypes(responseObjectType, _responseErrorType))
                request.Headers.Accept.Add(mediaType);

            if (responseObjectType is null)
                request.Headers.Accept.Add(AnyMediaType);
            else
                request.Properties[HttpRequestMessagePropertyKeys.ResponseObjectType] = responseObjectType;

            request.Properties[HttpRequestMessagePropertyKeys.TransactionId] = Guid.NewGuid();

            return request;
        }

        /// <summary>
        /// Disposes the <see cref="HttpRestClient"/> instance.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from a <see cref="IDisposable.Dispose()"/> method.</param>
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
                    if (_sharedHttpClient.Is(_httpClient))
                        _sharedHttpClient.Release(_httpClient);
                    else if (_disposeClient)
                        _httpClient.Dispose();
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
        /// This method is called as part of the disposal process of the <see cref="HttpRestClient"/> instance, specifically
        /// just before the client starts releasing its resources. It triggers the <see cref="Disposing"/> event, allowing
        /// subscribed entities to perform any necessary cleanup actions before the client is fully disposed.
        protected virtual void OnDisposing()
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}
