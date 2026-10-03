// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Content;
    using System;
    using System.IO;
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
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task<HttpResponseHeaders> HeadAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            using var response = await client.SendAsync(HttpVerb.Head, uri, payload: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Headers;
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
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task<HttpResponseHeaders> OptionsAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            using var response = await client.SendAsync(HttpVerb.Options, uri, payload: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Headers;
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
        public static async Task<byte[]> GetAsByteArrayAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            using var response = await client.SendAsync(HttpVerb.Get, uri, payload: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Content is not null ? await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false) : [];
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
        public static async Task<string> GetAsStringAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            using var response = await client.SendAsync(HttpVerb.Get, uri, payload: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Content is not null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
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
        public static async Task<Stream> GetAsStreamAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(HttpVerb.Get, uri, payload: null, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.Content is not null)
            {
                // The response is intentionally not disposed to avoid disposal of the underlying stream.
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }

            response.Dispose();
            return Stream.Null;
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
        public static async Task GetToStreamAsync(this HttpRestClient client, string uri, Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            using var response = await client.SendAsync(HttpVerb.Get, uri, payload: null, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.Content is not null)
            {
                using var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await body.CopyToAsync(stream, CopyBufferSize, cancellationToken).ConfigureAwait(false);
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
        public static async Task PostAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            using var _ = await client.SendAsync(HttpVerb.Post, uri, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
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
            return client.SendAsync<T>(HttpMethod.Put, uri, payload, cancellationToken);
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
        public static async Task PutAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            using var _ = await client.SendAsync(HttpVerb.Put, uri, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
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
        public static async Task PatchAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            using var _ = await client.SendAsync(HttpVerb.Patch, uri, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
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
        public static async Task DeleteAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            using var _ = await client.SendAsync(HttpVerb.Delete, uri, payload: null, cancellationToken: cancellationToken).ConfigureAwait(false);
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
        public static async Task<Stream> DownloadAsync
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

            using var response = await client.SendAsync(method, uri, payload, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Creates a new <see cref="HttpRequestScope"/> for managing scoped modifications of properties and headers for HTTP requests sent using the <see cref="HttpRestClient"/>.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance for which the scope is created.</param>
        /// <returns>An instance of <see cref="HttpRequestScope"/> that allows properties and headers to be temporarily modified for requests made through the client.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="client"/> argument is <see langword="null"/>>.</exception>
        public static HttpRequestScope WithScope(this HttpRestClient client)
        {
            return new HttpRequestScope(client);
        }
    }
}
