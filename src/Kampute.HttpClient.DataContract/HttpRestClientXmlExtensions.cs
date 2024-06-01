// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient.DataContract package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.DataContract
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="HttpRestClient"/> to support XML-based HTTP operations.
    /// </summary>
    /// <remarks>
    /// This static class extends <see cref="HttpRestClient"/> with methods tailored for handling HTTP requests and responses 
    /// involving XML data. It facilitates the sending and receiving of XML content by abstracting the complexities of serialization 
    /// and deserialization of XML to and from .NET objects.
    /// </remarks>
    public static class HttpRestClientXmlExtensions
    {
        private static readonly ConcurrentDictionary<HttpRestClient, DataContractSerializerSettings?> serializerSettings = new();

        private static void ClientDisposing(object sender, EventArgs e) => SetXmlSerializerSettings((HttpRestClient)sender, null);

        /// <summary>
        /// Configures the <see cref="HttpRestClient"/> to use the specified settings when serializing payloads as XML.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to configure.</param>
        /// <param name="settings">The <see cref="DataContractSerializerSettings"/> to use for serializing payload as XML. If <see langword="null"/>, default settings will be used.</param>
        public static void SetXmlSerializerSettings(this HttpRestClient client, DataContractSerializerSettings? settings)
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
        /// Retrieves the settings used by the <see cref="HttpRestClient"/> when serializing payloads as XML.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to query.</param>
        /// <returns>The <see cref="DataContractSerializerSettings"/> if set; otherwise, <see langword="null"/>.</returns>
        public static DataContractSerializerSettings? GetXmlSerializerSettings(this HttpRestClient client)
        {
            serializerSettings.TryGetValue(client, out var settings);
            return settings;
        }

        /// <summary>
        /// Configures the <see cref="HttpRestClient"/> to accept XML responses by adding or updating a <see cref="XmlContentDeserializer"/> in its response deserializers collection.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to configure.</param>
        /// <param name="settings">The <see cref="DataContractSerializerSettings"/> to use for deserializing XML responses. if <see langword="null"/>, default settings will be used.</param>
        /// <returns>The <see cref="XmlContentDeserializer"/> used for XML content deserialization.</returns>
        /// <remarks>
        /// If the client already has a <see cref="XmlContentDeserializer"/>, this method updates its options with the provided <paramref name="settings"/>.
        /// Otherwise, it adds a new <see cref="XmlContentDeserializer"/> with the specified options to the client's response deserializers.
        /// </remarks>
        public static XmlContentDeserializer AcceptXml(this HttpRestClient client, DataContractSerializerSettings? settings = null)
        {
            var deserializer = client.ResponseDeserializers.Find<XmlContentDeserializer>();
            if (deserializer is null)
            {
                deserializer = new XmlContentDeserializer();
                client.ResponseDeserializers.Add(deserializer);
            }
            deserializer.Settings = settings;
            return deserializer;
        }

        /// <summary>
        /// Sends an asynchronous request with XML-formatted payload to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of the object expected in the response.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> SendAsXmlAsync<T>
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

            var xmlContent = new XmlContent(payload) { Settings = client.GetXmlSerializerSettings() };
            return client.SendAsync<T>(method, uri, xmlContent, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with XML-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static async Task SendAsXmlAsync
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

            var xmlContent = new XmlContent(payload) { Settings = client.GetXmlSerializerSettings() };
            using var _ = await client.SendAsync(method, uri, xmlContent, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an asynchronous POST request with XML-formatted payload to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of the object expected in the response.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation, returning a deserialized object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PostAsXmlAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsXmlAsync<T>(HttpVerb.Post, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with XML-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PostAsXmlAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsXmlAsync(HttpVerb.Post, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with XML-formatted payload to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PutAsXmlAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsXmlAsync<T>(HttpVerb.Put, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with XML-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PutAsXmlAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsXmlAsync(HttpVerb.Put, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request with XML-formatted payload to the specified URI and returns the response body deserialized as the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation, with a result of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PatchAsXmlAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsXmlAsync<T>(HttpVerb.Patch, uri, payload, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request with XML-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PatchAsXmlAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendAsXmlAsync(HttpVerb.Patch, uri, payload, cancellationToken);
        }
    }
}
