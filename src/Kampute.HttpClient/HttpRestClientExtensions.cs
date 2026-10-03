// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Content;
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="HttpRestClient"/> to facilitate sending HTTP requests using various methods, 
    /// including GET, POST, PUT, PATCH, and DELETE.
    /// </summary>
    /// <remarks>
    /// This static class enriches <see cref="HttpRestClient"/> by adding convenient extension methods for making HTTP requests.
    /// These methods simplify the process of constructing and sending requests for common HTTP methods, enabling more readable 
    /// and concise client code.
    /// </remarks>
    public static class HttpRestClientExtensions
    {
        /// <summary>
        /// The buffer size used to copy a response body into a stream; the same default that <see cref="Stream.CopyToAsync(Stream)"/> uses.
        /// </summary>
        private const int CopyBufferSize = 81920;

        /// <summary>
        /// Sends an asynchronous HEAD request to the specified URI and returns the response headers.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning the response headers.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<HttpResponseHeaders> HeadAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return ReadHeadersAsync(client.SendAsync(HttpVerb.Head, uri, payload: null, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Sends an asynchronous OPTIONS request to the specified URI and returns the response headers.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning the response headers.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<HttpResponseHeaders> OptionsAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return ReadHeadersAsync(client.SendAsync(HttpVerb.Options, uri, payload: null, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Sends an asynchronous GET request to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> GetAsync<T>(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return client.SendAsync<T>(HttpVerb.Get, uri, payload: null, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous GET request to the specified URI and returns the response body as an array of bytes.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning an array of bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<byte[]> GetAsByteArrayAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return ReadBodyAsync(client.SendAsync(HttpVerb.Get, uri, payload: null, cancellationToken: cancellationToken));

            static async Task<byte[]> ReadBodyAsync(Task<HttpResponseMessage> sending)
            {
                using var response = await sending.ConfigureAwait(false);
                return response.Content is not null ? await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false) : [];
            }
        }

        /// <summary>
        /// Sends an asynchronous GET request to the specified URI and returns the response body as a string.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<string> GetAsStringAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return ReadBodyAsync(client.SendAsync(HttpVerb.Get, uri, payload: null, cancellationToken: cancellationToken));

            static async Task<string> ReadBodyAsync(Task<HttpResponseMessage> sending)
            {
                using var response = await sending.ConfigureAwait(false);
                return response.Content is not null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
            }
        }

        /// <summary>
        /// Sends an asynchronous GET request to the specified URI and returns the response body as a <see cref="Stream"/>.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, returning a <see cref="Stream"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// <para>
        /// The task completes as soon as the response headers arrive. The response body is not buffered: the returned stream reads it from the network,
        /// so the caller must dispose the stream to release the connection. Reading the stream can fail with an <see cref="IOException"/> if the transfer fails.
        /// </para>
        /// <para>
        /// <see cref="System.Net.Http.HttpClient.Timeout"/> covers only the time until the response headers arrive. Because the body is not buffered,
        /// an <see cref="HttpRestClient.AfterReceivingResponse"/> handler that reads the response content consumes the stream.
        /// </para>
        /// </remarks>
        public static Task<Stream> GetAsStreamAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return OpenBodyAsync(client.SendAsync(HttpVerb.Get, uri, payload: null, HttpCompletionOption.ResponseHeadersRead, cancellationToken));

            static async Task<Stream> OpenBodyAsync(Task<HttpResponseMessage> sending)
            {
                var response = await sending.ConfigureAwait(false);
                if (response.Content is not null)
                {
                    // The response is intentionally not disposed to avoid disposal of the underlying stream.
                    return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                }

                response.Dispose();
                return Stream.Null;
            }
        }

        /// <summary>
        /// Sends an asynchronous GET request to the specified URI and write the response body into the provided <see cref="Stream"/>.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="stream">The <see cref="Stream"/> where the response body is written.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="IOException">Thrown if transferring the response body fails, or writing to <paramref name="stream"/> fails.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// <para>
        /// The response body is not buffered: it is copied from the network into <paramref name="stream"/> as it arrives, and the cancellation token
        /// can stop the copy.
        /// </para>
        /// <para>
        /// <see cref="System.Net.Http.HttpClient.Timeout"/> covers only the time until the response headers arrive. Because the body is not buffered,
        /// an <see cref="HttpRestClient.AfterReceivingResponse"/> handler that reads the response content consumes it, and nothing is copied.
        /// </para>
        /// </remarks>
        public static Task GetToStreamAsync(this HttpRestClient client, string uri, Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            return CopyBodyAsync(client.SendAsync(HttpVerb.Get, uri, payload: null, HttpCompletionOption.ResponseHeadersRead, cancellationToken), stream, cancellationToken);

            static async Task CopyBodyAsync(Task<HttpResponseMessage> sending, Stream stream, CancellationToken cancellationToken)
            {
                using var response = await sending.ConfigureAwait(false);
                if (response.Content is not null)
                {
                    using var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    await body.CopyToAsync(stream, CopyBufferSize, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Sends an asynchronous POST request to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PostAsync<T>(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsync<T>(HttpVerb.Post, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PostAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return ReleaseResponseAsync(client.SendAsync(HttpVerb.Post, uri, payload, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Sends an asynchronous PUT request to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PutAsync<T>(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsync<T>(HttpVerb.Put, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PutAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return ReleaseResponseAsync(client.SendAsync(HttpVerb.Put, uri, payload, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Sends an asynchronous PATCH request to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PatchAsync<T>(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsync<T>(HttpVerb.Patch, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PatchAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return ReleaseResponseAsync(client.SendAsync(HttpVerb.Patch, uri, payload, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Sends an asynchronous DELETE request to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> DeleteAsync<T>(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return client.SendAsync<T>(HttpVerb.Delete, uri, payload: null, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous DELETE request to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task DeleteAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return ReleaseResponseAsync(client.SendAsync(HttpVerb.Delete, uri, payload: null, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Sends an asynchronous request whose payload is written in the specified media type by a registered content formatter, and returns the
        /// response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to send as the request payload. An <see cref="HttpContent"/> is sent as it is.</param>
        /// <param name="mediaType">The media type in which to write <paramref name="payload"/>.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="method"/>, <paramref name="uri"/>, <paramref name="payload"/> or <paramref name="mediaType"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no formatter in <see cref="HttpRestClient.ContentFormatters"/> can write <paramref name="payload"/> in <paramref name="mediaType"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// The payload is written by the first formatter in <see cref="HttpRestClient.ContentFormatters"/> that can write its type in <paramref name="mediaType"/>.
        /// The argument exceptions and the <see cref="InvalidOperationException"/> are thrown when the method is called, before anything is sent.
        /// </remarks>
        public static Task<T?> SendObjectAsync<T>(this HttpRestClient client, HttpMethod method, string uri, object payload, string mediaType, CancellationToken cancellationToken = default)
        {
            var content = CreateContent(client, method, uri, payload, mediaType);
            return client.SendAsync<T>(method, uri, content, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous request whose payload is written in the specified media type by a registered content formatter, without processing the
        /// response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to send as the request payload. An <see cref="HttpContent"/> is sent as it is.</param>
        /// <param name="mediaType">The media type in which to write <paramref name="payload"/>.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="method"/>, <paramref name="uri"/>, <paramref name="payload"/> or <paramref name="mediaType"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no formatter in <see cref="HttpRestClient.ContentFormatters"/> can write <paramref name="payload"/> in <paramref name="mediaType"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// The payload is written by the first formatter in <see cref="HttpRestClient.ContentFormatters"/> that can write its type in <paramref name="mediaType"/>.
        /// The argument exceptions and the <see cref="InvalidOperationException"/> are thrown when the method is called, before anything is sent.
        /// </remarks>
        public static Task SendObjectAsync(this HttpRestClient client, HttpMethod method, string uri, object payload, string mediaType, CancellationToken cancellationToken = default)
        {
            var content = CreateContent(client, method, uri, payload, mediaType);
            return ReleaseResponseAsync(client.SendAsync(method, uri, content, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Sends an asynchronous request whose payload is written by the specified content formatter, and returns the response body deserialized as the
        /// specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to send as the request payload. An <see cref="HttpContent"/> is sent as it is.</param>
        /// <param name="formatter">The formatter that writes <paramref name="payload"/>, in the first media type it can write for the type of the payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="method"/>, <paramref name="uri"/>, <paramref name="payload"/> or <paramref name="formatter"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="formatter"/> cannot write the type of <paramref name="payload"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// The formatters in <see cref="HttpRestClient.ContentFormatters"/> are not consulted to write the payload, so the payload is written by
        /// <paramref name="formatter"/> even when another formatter is registered for the same media type. The response is still read by the registered
        /// formatters. The argument exceptions and the <see cref="InvalidOperationException"/> are thrown when the method is called, before anything is sent.
        /// </remarks>
        public static Task<T?> SendObjectAsync<T>(this HttpRestClient client, HttpMethod method, string uri, object payload, IHttpContentFormatter formatter, CancellationToken cancellationToken = default)
        {
            var content = CreateContent(client, method, uri, payload, formatter);
            return client.SendAsync<T>(method, uri, content, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous request whose payload is written by the specified content formatter, without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to send as the request payload. An <see cref="HttpContent"/> is sent as it is.</param>
        /// <param name="formatter">The formatter that writes <paramref name="payload"/>, in the first media type it can write for the type of the payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="method"/>, <paramref name="uri"/>, <paramref name="payload"/> or <paramref name="formatter"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="formatter"/> cannot write the type of <paramref name="payload"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// The formatters in <see cref="HttpRestClient.ContentFormatters"/> are not consulted, so the payload is written by <paramref name="formatter"/> even
        /// when another formatter is registered for the same media type. The argument exceptions and the <see cref="InvalidOperationException"/> are thrown when
        /// the method is called, before anything is sent.
        /// </remarks>
        public static Task SendObjectAsync(this HttpRestClient client, HttpMethod method, string uri, object payload, IHttpContentFormatter formatter, CancellationToken cancellationToken = default)
        {
            var content = CreateContent(client, method, uri, payload, formatter);
            return ReleaseResponseAsync(client.SendAsync(method, uri, content, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Sends an asynchronous HTTP request with the specified method, URI, and payload, returning the response content as a stream.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content.</param>
        /// <param name="streamProvider">A function that returns a <see cref="Stream"/> based on the HTTP content headers.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Stream"/> that represents the response content.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/>, or <paramref name="streamProvider"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="streamProvider"/> returns <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="IOException">Thrown if transferring the response body fails, or writing to the stream from <paramref name="streamProvider"/> fails.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// <para>
        /// The response body is not buffered: it is copied from the network into the stream returned by <paramref name="streamProvider"/> as it
        /// arrives, and the cancellation token can stop the copy. If the copy fails or is canceled, that stream is disposed before the exception
        /// is rethrown.
        /// </para>
        /// <para>
        /// <see cref="System.Net.Http.HttpClient.Timeout"/> covers only the time until the response headers arrive. Because the body is not buffered,
        /// an <see cref="HttpRestClient.AfterReceivingResponse"/> handler that reads the response content consumes it, and nothing is copied.
        /// </para>
        /// </remarks>
        public static Task<Stream> DownloadAsync
        (
            this HttpRestClient client,
            HttpMethod method,
            string uri,
            HttpContent? payload,
            Func<HttpContentHeaders, Stream> streamProvider,
            CancellationToken cancellationToken = default
        )
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));
            if (streamProvider is null)
                throw new ArgumentNullException(nameof(streamProvider));

            return CopyBodyAsync(client.SendAsync(method, uri, payload, HttpCompletionOption.ResponseHeadersRead, cancellationToken), streamProvider, cancellationToken);

            static async Task<Stream> CopyBodyAsync(Task<HttpResponseMessage> sending, Func<HttpContentHeaders, Stream> streamProvider, CancellationToken cancellationToken)
            {
                using var response = await sending.ConfigureAwait(false);
                response.Content ??= new EmptyContent();

                var stream = streamProvider(response.Content.Headers) ?? throw new InvalidOperationException("The stream provider must not return null.");
                try
                {
                    using var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    await body.CopyToAsync(stream, CopyBufferSize, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
                return stream;
            }
        }

        /// <summary>
        /// Creates a new <see cref="HttpRequestScope"/> for managing scoped modifications of properties and headers for HTTP requests sent using the <see cref="HttpRestClient"/>.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance for which the scope is created.</param>
        /// <returns>An instance of <see cref="HttpRequestScope"/> that allows properties and headers to be temporarily modified for requests made through the client.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="client"/> argument is <see langword="null"/>.</exception>
        public static HttpRequestScope WithScope(this HttpRestClient client)
        {
            return new HttpRequestScope(client);
        }

        /// <summary>
        /// Waits for a request to complete and disposes of its response.
        /// </summary>
        /// <param name="sending">The task of the request that is being sent.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        internal static async Task ReleaseResponseAsync(Task<HttpResponseMessage> sending)
        {
            using var _ = await sending.ConfigureAwait(false);
        }

        /// <summary>
        /// Validates the arguments of a <c>SendObjectAsync</c> call and creates the request content with the registered formatter for the media type.
        /// </summary>
        /// <param name="client">The client that sends the request.</param>
        /// <param name="method">The HTTP method of the request.</param>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="payload">The object to send.</param>
        /// <param name="mediaType">The media type in which to write <paramref name="payload"/>.</param>
        /// <returns>The request content.</returns>
        private static HttpContent CreateContent(HttpRestClient client, HttpMethod method, string uri, object payload, string mediaType)
        {
            ValidateSendArguments(client, method, uri, payload);
            if (mediaType is null)
                throw new ArgumentNullException(nameof(mediaType));

            if (payload is HttpContent content)
                return content;

            var formatter = client.ContentFormatters.GetWriterFor(mediaType, payload.GetType())
                ?? throw new InvalidOperationException($"No content formatter of the client can write an object of type '{payload.GetType()}' as '{mediaType}'.");

            return formatter.Write(payload, mediaType);
        }

        /// <summary>
        /// Validates the arguments of a <c>SendObjectAsync</c> call and creates the request content with the specified formatter.
        /// </summary>
        /// <param name="client">The client that sends the request.</param>
        /// <param name="method">The HTTP method of the request.</param>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="payload">The object to send.</param>
        /// <param name="formatter">The formatter that writes <paramref name="payload"/>.</param>
        /// <returns>The request content.</returns>
        private static HttpContent CreateContent(HttpRestClient client, HttpMethod method, string uri, object payload, IHttpContentFormatter formatter)
        {
            ValidateSendArguments(client, method, uri, payload);
            if (formatter is null)
                throw new ArgumentNullException(nameof(formatter));

            if (payload is HttpContent content)
                return content;

            var mediaType = formatter.GetWritableMediaTypes(payload.GetType()).FirstOrDefault()
                ?? throw new InvalidOperationException($"{formatter.GetType().Name} cannot write an object of type '{payload.GetType()}'.");

            return formatter.Write(payload, mediaType);
        }

        /// <summary>
        /// Validates the arguments shared by all <c>SendObjectAsync</c> overloads.
        /// </summary>
        /// <param name="client">The client that sends the request.</param>
        /// <param name="method">The HTTP method of the request.</param>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="payload">The object to send.</param>
        private static void ValidateSendArguments(HttpRestClient client, HttpMethod method, string uri, object payload)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));
            if (payload is null)
                throw new ArgumentNullException(nameof(payload));
        }

        /// <summary>
        /// Waits for a request to complete, disposes of its response, and returns the response headers.
        /// </summary>
        /// <param name="sending">The task of the request that is being sent.</param>
        /// <returns>A task that represents the asynchronous operation, returning the response headers.</returns>
        private static async Task<HttpResponseHeaders> ReadHeadersAsync(Task<HttpResponseMessage> sending)
        {
            using var response = await sending.ConfigureAwait(false);
            return response.Headers;
        }
    }
}
