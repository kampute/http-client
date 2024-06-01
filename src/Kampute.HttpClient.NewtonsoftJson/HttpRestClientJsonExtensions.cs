// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient.NewtonsoftJson package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.NewtonsoftJson
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Net.Http;
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
        private static readonly ConcurrentDictionary<HttpRestClient, JsonSerializerSettings?> serializerSettings = new();

        private static void ClientDisposing(object sender, EventArgs e) => SetJsonSerializerSettings((HttpRestClient)sender, null);

        /// <summary>
        /// Configures the <see cref="HttpRestClient"/> to use the specified settings when serializing payloads as JSON.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to configure.</param>
        /// <param name="settings">The <see cref="JsonSerializerSettings"/> to use for serializing payload as JSON. if <see langword="null"/>, default settings will be used.</param>
        public static void SetJsonSerializerSettings(this HttpRestClient client, JsonSerializerSettings? settings)
        {
            client.Disposing -= ClientDisposing;
            if (settings is not null)
            {
                serializerSettings[client] = settings;
                client.Disposing += ClientDisposing;
            }
            else
            {
                serializerSettings.TryRemove(client, out _);
            }
        }

        /// <summary>
        /// Retrieves the settings used by the <see cref="HttpRestClient"/> when serializing payloads as JSON.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to query.</param>
        /// <returns>The <see cref="JsonSerializerSettings"/> if set; otherwise, <see langword="null"/>.</returns>
        public static JsonSerializerSettings? GetJsonSerializerSettings(this HttpRestClient client)
        {
            serializerSettings.TryGetValue(client, out var settings);
            return settings;
        }

        /// <summary>
        /// Configures the <see cref="HttpRestClient"/> to accept JSON responses by adding or updating a <see cref="JsonContentDeserializer"/> in its response deserializers collection.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to configure.</param>
        /// <param name="settings">The <see cref="JsonSerializerSettings"/> to use for deserializing JSON responses. if <see langword="null"/>, default settings will be used.</param>
        /// <returns>The <see cref="JsonContentDeserializer"/> used for JSON content deserialization.</returns>
        /// <remarks>
        /// If the client already has a <see cref="JsonContentDeserializer"/>, this method updates its settings with the provided <paramref name="settings"/>.
        /// Otherwise, it adds a new <see cref="JsonContentDeserializer"/> with the specified settings to the client's response deserializers.
        /// </remarks>
        public static JsonContentDeserializer AcceptJson(this HttpRestClient client, JsonSerializerSettings? settings = null)
        {
            var deserializer = client.ResponseDeserializers.Find<JsonContentDeserializer>();
            if (deserializer is null)
            {
                deserializer = new JsonContentDeserializer();
                client.ResponseDeserializers.Add(deserializer);
            }
            deserializer.Settings = settings;
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> SendAsJsonAsync<T>
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

            var jsonContent = new JsonContent(payload) { Settings = client.GetJsonSerializerSettings() };
            return client.SendAsync<T>(method, uri, jsonContent, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with JSON-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the JSON-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task SendAsJsonAsync
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

            var jsonContent = new JsonContent(payload) { Settings = client.GetJsonSerializerSettings() };
            using var _ = await client.SendAsync(method, uri, jsonContent, cancellationToken).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PatchAsJsonAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsJsonAsync(HttpVerb.Patch, uri, payload, cancellationToken);
        }
    }
}
