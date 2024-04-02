// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
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
        /// Sends an asynchronous GET request to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T?> GetAsync<T>(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return client.SendAsync<T>(HttpVerb.Get, uri, default, cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PostAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsync(HttpVerb.Post, uri, payload, cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PutAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsync(HttpVerb.Put, uri, payload, cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PatchAsync(this HttpRestClient client, string uri, HttpContent? payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsync(HttpVerb.Patch, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous DELETE request to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T?> DeleteAsync<T>(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return client.SendAsync<T>(HttpVerb.Delete, uri, default, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous DELETE request to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task DeleteAsync(this HttpRestClient client, string uri, CancellationToken cancellationToken = default)
        {
            return client.SendAsync(HttpVerb.Delete, uri, default, cancellationToken);
        }

        /// <summary>
        /// Retrieves data from the specified URI and writes it to the provided stream asynchronously.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="stream">The stream to write the downloaded data to. It must be writable.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The HTTP request payload content (optional).</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>
        /// A task that represents the asynchronous download operation. The task result contains the headers of the downloaded content. 
        /// If the response contains no content, <c>null</c> is returned.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/>, <paramref name="method"/>, or <paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task<HttpContentHeaders?> FetchToStreamAsync
        (
            this HttpRestClient client,
            Stream stream,
            HttpMethod method,
            string uri,
            HttpContent? payload = default,
            CancellationToken cancellationToken = default
        )
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            var contentHeaders = default(HttpContentHeaders);
            await client.DownloadAsync(method, uri, payload, headers =>
            {
                contentHeaders = headers;
                return stream;
            }, cancellationToken);
            return contentHeaders;
        }
    }
}
