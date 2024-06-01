namespace Kampute.HttpClient.Content.Abstracts
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides functionality for deserializing content from HTTP responses into objects.
    /// </summary>
    public abstract class HttpContentDeserializer : IHttpContentDeserializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentDeserializer"/> class with specified supported media types.
        /// </summary>
        /// <param name="supportedMediaTypes">An array of media types that this deserializer supports.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="supportedMediaTypes"/> is <see langword="null"/>.</exception>
        protected HttpContentDeserializer(params string[] supportedMediaTypes)
        {
            SupportedMediaTypes = supportedMediaTypes ?? throw new ArgumentNullException(nameof(supportedMediaTypes));
        }

        /// <summary>
        /// Gets the collection of media types that this deserializer supports.
        /// </summary>
        /// <value>The read-only collection of media types that this deserializer can handle.</value>
        public IReadOnlyCollection<string> SupportedMediaTypes { get; }

        /// <summary>
        /// Retrieves a collection of supported media types for a specific model type.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <returns>An enumerable of strings representing the media types supported for the specified model type.</returns>
        public virtual IEnumerable<string> GetSupportedMediaTypes(Type modelType)
        {
            return modelType is not null ? SupportedMediaTypes : [];
        }

        /// <summary>
        /// Determines whether this deserializer can handle data of a specific media type and deserialize it into the specified model type.
        /// </summary>
        /// <param name="mediaType">The media type of the content.</param>
        /// <param name="modelType">The target model type for deserialization.</param>
        /// <returns><see langword="true"/> if the deserializer supports the media type and the model type is not <see langword="null"/>; otherwise, <see langword="false"/>.</returns>
        public virtual bool CanDeserialize(string mediaType, Type modelType)
        {
            return SupportedMediaTypes.Contains(mediaType);
        }

        /// <summary>
        /// Asynchronously reads and deserializes an object from the provided <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to read from.</param>
        /// <param name="modelType">The type of the object to be deserialized.</param>
        /// <param name="cancellationToken">A token for canceling the read operation (optional).</param>
        /// <returns>A task representing the asynchronous read operation, which upon completion contains the deserialized object, or <see langword="null"/> if deserialization fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="content"/> or <paramref name="modelType"/> is <see langword="null"/>.</exception>
        public abstract Task<object?> DeserializeAsync(HttpContent content, Type modelType, CancellationToken cancellationToken = default);
    }
}
