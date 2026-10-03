// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient.Json package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Json
{
    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="HttpRestClient"/> to support JSON-based HTTP operations.
    /// </summary>
    /// <remarks>
    /// <see cref="UseJson"/> registers a <see cref="JsonFormatter"/>, which lets the client read JSON responses and advertise JSON in the <c>Accept</c>
    /// header. The send helpers write their payloads with the registered <see cref="JsonFormatter"/>, or with a new one with default options if none
    /// is registered.
    /// </remarks>
    public static class HttpRestClientJsonExtensions
    {
        /// <summary>
        /// Registers a <see cref="JsonFormatter"/> with the client, or updates the registered one.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to configure.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for reading responses and writing payloads. If <see langword="null"/>, default options are used.</param>
        /// <returns>The registered <see cref="JsonFormatter"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If the client already has a <see cref="JsonFormatter"/>, this method sets its options to <paramref name="options"/>. Otherwise, it adds a new
        /// <see cref="JsonFormatter"/> with <paramref name="options"/> to <see cref="HttpRestClient.ContentFormatters"/>.
        /// </remarks>
        public static JsonFormatter UseJson(this HttpRestClient client, JsonSerializerOptions? options = null)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            var formatter = client.ContentFormatters.Find<JsonFormatter>();
            if (formatter is null)
            {
                formatter = new JsonFormatter();
                client.ContentFormatters.Add(formatter);
            }
            formatter.Options = options;
            return formatter;
        }

        /// <summary>
        /// Sends an asynchronous request with JSON-formatted payload to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of the object expected in the response.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> SendAsJsonAsync<T>(this HttpRestClient client, HttpMethod method, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync<T>(method, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task SendAsJsonAsync(this HttpRestClient client, HttpMethod method, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync(method, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with JSON-formatted payload to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of the object expected in the response.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PostAsJsonAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync<T>(HttpVerb.Post, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PostAsJsonAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync(HttpVerb.Post, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with JSON-formatted payload to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PutAsJsonAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync<T>(HttpVerb.Put, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PutAsJsonAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync(HttpVerb.Put, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request with JSON-formatted payload to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PatchAsJsonAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync<T>(HttpVerb.Patch, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PatchAsJsonAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync(HttpVerb.Patch, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Returns the registered <see cref="JsonFormatter"/> of the client, or a new one with default options.
        /// </summary>
        /// <param name="client">The client whose formatter to return.</param>
        /// <returns>The <see cref="JsonFormatter"/> that writes the payloads of the client.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> is <see langword="null"/>.</exception>
        private static JsonFormatter FormatterOf(HttpRestClient client)
        {
            return client is not null
                ? client.ContentFormatters.FindOrDefault<JsonFormatter>()
                : throw new ArgumentNullException(nameof(client));
        }
    }
}
