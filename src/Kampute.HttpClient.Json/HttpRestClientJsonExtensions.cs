// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient.Json package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Json
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="HttpRestClient"/> to support JSON-based HTTP operations.
    /// </summary>
    /// <remarks>
    /// This static class enhances <see cref="HttpRestClient"/> by offering methods specifically designed for handling HTTP 
    /// requests and responses that involve JSON data. It simplifies the process of sending and receiving JSON content, by 
    /// abstracting the serialization and deserialization of JSON to and from .NET objects.
    /// </remarks>
    public static class HttpRestClientJsonExtensions
    {
        private static readonly ConcurrentDictionary<HttpRestClient, JsonSerializerOptions?> serializerOptions = new();

        private static void ClientDisposing(object sender, EventArgs e) => SetJsonSerializerOptions((HttpRestClient)sender, null);

        /// <summary>
        /// Configures the <see cref="HttpRestClient"/> to use the specified options when serializing payloads as JSON.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to configure.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serializing payload as JSON. if <c>null</c>, default options will be used.</param>
        public static void SetJsonSerializerOptions(this HttpRestClient client, JsonSerializerOptions? options)
        {
            client.Disposing -= ClientDisposing;
            if (options is not null)
            {
                serializerOptions[client] = options;
                client.Disposing += ClientDisposing;
            }
            else
            {
                serializerOptions.TryRemove(client, out _);
            }
        }

        /// <summary>
        /// Retrieves the options used by the <see cref="HttpRestClient"/> when serializing payloads as JSON.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to query.</param>
        /// <returns>The <see cref="JsonSerializerOptions"/> if set; otherwise, <c>null</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JsonSerializerOptions? GetJsonSerializerOptions(this HttpRestClient client)
        {
            serializerOptions.TryGetValue(client, out var options);
            return options;
        }

        /// <summary>
        /// Configures the <see cref="HttpRestClient"/> to accept JSON responses by adding or updating a <see cref="JsonContentDeserializer"/> in its response deserializers collection.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to configure.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserializing JSON responses. if <c>null</c>, default options will be used.</param>
        /// <returns>The <see cref="JsonContentDeserializer"/> used for JSON content deserialization.</returns>
        /// <remarks>
        /// If the client already has a <see cref="JsonContentDeserializer"/>, this method updates its options with the provided <paramref name="options"/>.
        /// Otherwise, it adds a new <see cref="JsonContentDeserializer"/> with the specified options to the client's response deserializers.
        /// </remarks>
        public static JsonContentDeserializer AcceptJson(this HttpRestClient client, JsonSerializerOptions? options = null)
        {
            var deserializer = client.ResponseDeserializers.Find<JsonContentDeserializer>();
            if (deserializer is null)
            {
                deserializer = new JsonContentDeserializer();
                client.ResponseDeserializers.Add(deserializer);
            }
            deserializer.Options = options;
            return deserializer;
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task<T?> SendAsJsonAsync<T>
        (
            this HttpRestClient client,
            HttpMethod method,
            string uri,
            object payload,
            CancellationToken cancellationToken = default
        )
        {
            if (payload is null)
                throw new ArgumentNullException(nameof(payload));

            using var content = new JsonContent(payload) { Options = client.GetJsonSerializerOptions() };
            return await client.SendAsync<T>(method, uri, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an asynchronous POST request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning headers of the response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task<HttpResponseHeaders> SendAsJsonAsync
        (
            this HttpRestClient client,
            HttpMethod method,
            string uri,
            object payload,
            CancellationToken cancellationToken = default
        )
        {
            if (payload is null)
                throw new ArgumentNullException(nameof(payload));

            using var content = new JsonContent(payload) { Options = client.GetJsonSerializerOptions() };
            return await client.SendAsync(method, uri, content, cancellationToken).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T?> PostAsJsonAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsJsonAsync<T>(HttpVerb.Post, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PostAsJsonAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsJsonAsync(HttpVerb.Post, uri, payload, cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T?> PutAsJsonAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsJsonAsync<T>(HttpVerb.Put, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PutAsJsonAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsJsonAsync(HttpVerb.Put, uri, payload, cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T?> PatchAsJsonAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsJsonAsync<T>(HttpVerb.Patch, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <c>null</c>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task PatchAsJsonAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsJsonAsync(HttpVerb.Patch, uri, payload, cancellationToken);
        }
    }
}
