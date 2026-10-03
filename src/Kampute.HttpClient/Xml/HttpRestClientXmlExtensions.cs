// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Xml
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="HttpRestClient"/> to support XML-based HTTP operations.
    /// </summary>
    /// <remarks>
    /// <see cref="UseXml"/> registers an <see cref="XmlFormatter"/>, which lets the client read XML responses and advertise XML in the <c>Accept</c>
    /// header. The send helpers write their payloads with the registered <see cref="XmlFormatter"/>, or with a new one with default settings if
    /// none is registered.
    /// </remarks>
    public static class HttpRestClientXmlExtensions
    {
        /// <summary>
        /// Registers an <see cref="XmlFormatter"/> with the client, or updates the registered one.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to configure.</param>
        /// <param name="configure">An action that sets the options of the formatter, such as <see cref="XmlFormatter.Serializer"/> (optional).</param>
        /// <returns>The registered <see cref="XmlFormatter"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If the client already has an <see cref="XmlFormatter"/>, this method passes that formatter to <paramref name="configure"/>. Otherwise, it
        /// adds a new <see cref="XmlFormatter"/> to <see cref="HttpRestClient.ContentFormatters"/> and passes the new one.
        /// </remarks>
        public static XmlFormatter UseXml(this HttpRestClient client, Action<XmlFormatter>? configure = null)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            var formatter = client.ContentFormatters.Find<XmlFormatter>();
            if (formatter is null)
            {
                formatter = new XmlFormatter();
                client.ContentFormatters.Add(formatter);
            }
            configure?.Invoke(formatter);
            return formatter;
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> SendAsXmlAsync<T>(this HttpRestClient client, HttpMethod method, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync<T>(method, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous request with XML-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="method"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task SendAsXmlAsync(this HttpRestClient client, HttpMethod method, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync(method, uri, payload, FormatterOf(client), cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PostAsXmlAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync<T>(HttpVerb.Post, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with XML-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PostAsXmlAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync(HttpVerb.Post, uri, payload, FormatterOf(client), cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PutAsXmlAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync<T>(HttpVerb.Put, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with XML-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PutAsXmlAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync(HttpVerb.Put, uri, payload, FormatterOf(client), cancellationToken);
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="HttpContentException">Thrown if the response body is empty or its media type is not supported.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task<T?> PatchAsXmlAsync<T>(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync<T>(HttpVerb.Patch, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PATCH request with XML-formatted payload to the specified URI without processing the response body.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> instance to be used for sending the request.</param>
        /// <param name="uri">The URI to which the request is sent.</param>
        /// <param name="payload">The object to serialize as the XML-formatted HTTP request payload.</param>
        /// <param name="cancellationToken">A token for canceling the request (optional).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/>, <paramref name="uri"/> or <paramref name="payload"/> is <see langword="null"/>.</exception>
        /// <exception cref="HttpResponseException">Thrown if the response status code indicates a failure.</exception>
        /// <exception cref="HttpRequestException">Thrown if the request fails due to an underlying issue such as network connectivity, DNS failure, server certificate validation, or timeout.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        public static Task PatchAsXmlAsync(this HttpRestClient client, string uri, object payload, CancellationToken cancellationToken = default)
        {
            return client.SendObjectAsync(HttpVerb.Patch, uri, payload, FormatterOf(client), cancellationToken);
        }

        /// <summary>
        /// Returns the registered <see cref="XmlFormatter"/> of the client, or a new one with default settings.
        /// </summary>
        /// <param name="client">The client whose formatter to return.</param>
        /// <returns>The <see cref="XmlFormatter"/> that writes the payloads of the client.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> is <see langword="null"/>.</exception>
        private static XmlFormatter FormatterOf(HttpRestClient client)
        {
            return client is not null
                ? client.ContentFormatters.FindOrDefault<XmlFormatter>()
                : throw new ArgumentNullException(nameof(client));
        }
    }
}
