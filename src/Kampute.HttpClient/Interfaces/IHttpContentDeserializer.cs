// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the functionality for deserializing an object from the HTTP request body.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="IHttpContentDeserializer"/> interface is designed for the purpose of abstracting the mechanism of deserializing data 
    /// from HTTP responses into .NET objects. Implementers of this interface provide the logic necessary to convert HTTP content, identified 
    /// by a specific media type, into instances of model types used within an application.
    /// </para>
    /// <para>
    /// The <see cref="GetSupportedMediaTypes"/> method is designed to communicate the content types that the deserializer can process, effectively 
    /// informing the server about the media types acceptable to the client. It ensures that the client and server can agree on a common format for 
    /// data exchange, enhancing the efficiency and compatibility of HTTP communications.
    /// </para>
    /// <para>
    /// Through the <see cref="CanDeserialize"/> method, implementations can provide a quick check to ascertain compatibility between the deserializer, 
    /// the media type of the content, and the target model type. This check is typically performed before attempting deserialization to ensure that 
    /// the deserializer is capable of processing the content as expected.
    /// </para>
    /// <para>
    /// The asynchronous <see cref="DeserializeAsync"/> method forms the core of the interface, where the actual deserialization logic is implemented. 
    /// This method takes <see cref="HttpContent"/>, along with the target model type, and returns a task that, when completed, yields the deserialized 
    /// object. Implementations must handle the asynchronous nature of this operation, catering to potential cancellation requests through the 
    /// provided <see cref="CancellationToken"/> provided by the parameter.
    /// </para>
    /// <para>
    /// The implementations of <see cref="IHttpContentDeserializer"/> should be thread-safe and reusable across multiple deserialization operations to 
    /// facilitate efficient processing of HTTP response content in a concurrent environment.
    /// </para>
    /// </remarks>
    /// <seealso cref="HttpRestClient.ResponseDeserializers"/>
    public interface IHttpContentDeserializer
    {
        /// <summary>
        /// Retrieves a collection of supported media types for a specific model type.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <returns>An enumerable of strings representing the media types supported for the specified model type.</returns>
        IEnumerable<string> GetSupportedMediaTypes(Type modelType);

        /// <summary>
        /// Determines whether this deserializer can handle data of a specific content type and deserialize it into the specified model type.
        /// </summary>
        /// <param name="mediaType">The media type of the content.</param>
        /// <param name="modelType">The type of the model to be deserialized.</param>
        /// <returns><c>true</c> if this deserializer can handle the specified content type and model type; otherwise, <c>false</c>.</returns>
        bool CanDeserialize(string mediaType, Type modelType);

        /// <summary>
        /// Asynchronously deserializes an object from the provided <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> from which to deserialize the data.</param>
        /// <param name="modelType">The type of the object to be deserialized.</param>
        /// <param name="cancellationToken">A token for canceling the operation (optional).</param>
        /// <returns>A task representing the asynchronous deserialization operation. Contains the deserialized object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> or <paramref name="modelType"/> is <c>null</c>.</exception>
        Task<object?> DeserializeAsync(HttpContent content, Type modelType, CancellationToken cancellationToken = default);
    }
}
