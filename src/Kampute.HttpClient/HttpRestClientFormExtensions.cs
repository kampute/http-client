// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="HttpRestClient"/> to support sending HTTP requests with URL-encoded form content.
    /// </summary>
    /// <remarks>
    /// This static class extends <see cref="HttpRestClient"/> functionality by adding methods for sending HTTP requests with content 
    /// type 'application/x-www-form-urlencoded'. 
    /// </remarks>
    public static class HttpRestClientFormExtensions
    {
        /// <summary>
        /// Sends an asynchronous request with URL-encoded form content to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of the object expected in the response.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The collection of key-value pairs to serialize as the URL-encoded HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task<T?> SendAsFormAsync<T>
        (
            this HttpRestClient client,
            HttpMethod method,
            string uri,
            IEnumerable<KeyValuePair<string, string>> payload,
            CancellationToken cancellationToken = default
        )
        {
            if (payload is null)
                throw new ArgumentNullException(nameof(payload));

            using var content = new FormUrlEncodedContent(payload);
            return await client.SendAsync<T>(method, uri, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an asynchronous POST request with URL-encoded form content to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The collection of key-value pairs to serialize as the URL-encoded HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning headers of the response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task<HttpResponseHeaders> SendAsFormAsync
        (
            this HttpRestClient client,
            HttpMethod method,
            string uri,
            IEnumerable<KeyValuePair<string, string>> payload,
            CancellationToken cancellationToken = default
        )
        {
            if (payload is null)
                throw new ArgumentNullException(nameof(payload));

            using var content = new FormUrlEncodedContent(payload);
            return await client.SendAsync(method, uri, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an asynchronous POST request with URL-encoded form content to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of the object expected in the response.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The collection of key-value pairs to serialize as the URL-encoded HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T?> PostAsFormAsync<T>
        (
            this HttpRestClient client,
            string uri,
            IEnumerable<KeyValuePair<string, string>> payload,
            CancellationToken cancellationToken = default
        )
        {
            return client.SendAsFormAsync<T>(HttpVerb.Post, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with URL-encoded form content to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The collection of key-value pairs to serialize as the URL-encoded HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PostAsFormAsync
        (
            this HttpRestClient client,
            string uri,
            IEnumerable<KeyValuePair<string, string>> payload,
            CancellationToken cancellationToken = default
        )
        {
            return client.SendAsFormAsync(HttpVerb.Post, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with URL-encoded form content to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The collection of key-value pairs to serialize as the URL-encoded HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T?> PutAsFormAsync<T>
        (
            this HttpRestClient client,
            string uri,
            IEnumerable<KeyValuePair<string, string>> payload,
            CancellationToken cancellationToken = default
        )
        {
            return client.SendAsFormAsync<T>(HttpVerb.Put, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with URL-encoded form content to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The collection of key-value pairs to serialize as the URL-encoded HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PutAsFormAsync
        (
            this HttpRestClient client,
            string uri,
            IEnumerable<KeyValuePair<string, string>> payload,
            CancellationToken cancellationToken = default
        )
        {
            return client.SendAsFormAsync(HttpVerb.Put, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request with URL-encoded form content to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The collection of key-value pairs to serialize as the URL-encoded HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T?> PatchAsFormAsync<T>
        (
            this HttpRestClient client,
            string uri,
            IEnumerable<KeyValuePair<string, string>> payload,
            CancellationToken cancellationToken = default
        )
        {
            return client.SendAsFormAsync<T>(HttpVerb.Patch, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request with URL-encoded form content to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The collection of key-value pairs to serialize as the URL-encoded HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the content type of the response is either unknown or not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PatchAsFormAsync
        (
            this HttpRestClient client,
            string uri,
            IEnumerable<KeyValuePair<string, string>> payload,
            CancellationToken cancellationToken = default
        )
        {
            return client.SendAsFormAsync(HttpVerb.Patch, uri, payload, cancellationToken);
        }
    }
}
