// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a specialized collection of <see cref="IHttpContentDeserializer"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This collection provides capabilities for managing <see cref="IHttpContentDeserializer"/> instances, such as adding, 
    /// removing, and selecting deserializers. It utilizes internal caches to optimize performance when retrieving deserializers 
    /// for specific media types and model types, as well as when fetching supported media types for deserialization. 
    /// </para>
    /// <para>
    /// These caches are automatically invalidated and updated as deserializers are added or removed, ensuring efficient access 
    /// patterns and reducing the overhead of frequently performed operations.
    /// </para>
    /// </remarks>
    public sealed class HttpContentDeserializerCollection : ICollection<IHttpContentDeserializer>
    {
        private static readonly ConcurrentDictionary<string, MediaTypeWithQualityHeaderValue> _mediaTypeHeaderValues = new();

        private readonly List<IHttpContentDeserializer> _collection = [];
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, IReadOnlyCollection<IHttpContentDeserializer>>> _deserializersCache = new();
        private readonly ConcurrentDictionary<Type, IReadOnlyCollection<MediaTypeWithQualityHeaderValue>> _mediaTypes1Cache = new();
        private readonly ConcurrentDictionary<(Type, Type), IReadOnlyCollection<MediaTypeWithQualityHeaderValue>> _mediaTypes2Cache = new();

        /// <summary>
        /// Gets the number of <see cref="IHttpContentDeserializer"/> instances contained in the collection.
        /// </summary>
        /// <value>
        /// The number of <see cref="IHttpContentDeserializer"/> instances contained in the collection.
        /// </value>
        public int Count => _collection.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only. Always returns <c>false</c> for this implementation.
        /// </summary>
        /// <value>
        /// Indicates whether the collection is read-only. This implementation always returns <c>false</c>.
        /// </value> 
        bool ICollection<IHttpContentDeserializer>.IsReadOnly => false;

        /// <summary>
        /// Retrieves all <see cref="IHttpContentDeserializer"/> instances in the collection that support deserializing a specific media type and model type.
        /// </summary>
        /// <param name="mediaType">The media type to deserialize.</param>
        /// <param name="modelType">The type of the model to deserialize.</param>
        /// <returns>A read-only collection of <see cref="IHttpContentDeserializer"/> that can deserialize the specified media type and model type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyCollection<IHttpContentDeserializer> For(string mediaType, Type modelType)
        {
            return mediaType is not null && modelType is not null
                ? _deserializersCache
                    .GetOrAdd(mediaType, _ => new ConcurrentDictionary<Type, IReadOnlyCollection<IHttpContentDeserializer>>())
                    .GetOrAdd(modelType, _ => _collection.Where(deserializer => deserializer.CanDeserialize(mediaType, modelType)).ToArray())
                : Array.Empty<IHttpContentDeserializer>();
        }

        /// <summary>
        /// Retrieves all supported media types for a specified model type from the collection of deserializers.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <returns>A read-only collection of <see cref="MediaTypeWithQualityHeaderValue"/> that represent the media types supported for deserializing the specified model type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyCollection<MediaTypeWithQualityHeaderValue> GetSupportedMediaTypes(Type? modelType)
        {
            return modelType is not null
                ? _mediaTypes1Cache.GetOrAdd(modelType, _ => _collection
                    .SelectMany(deserializer => deserializer.GetSupportedMediaTypes(modelType))
                    .Distinct()
                    .Select(MediaTypeHeaderValue)
                    .ToArray())
                : Array.Empty<MediaTypeWithQualityHeaderValue>();
        }

        /// <summary>
        /// Retrieves all supported media types for a specified model type and error type from the collection of deserializers.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <param name="errorType">The type of the error for which to retrieve supported media types.</param>
        /// <returns>A read-only collection of <see cref="MediaTypeWithQualityHeaderValue"/> representing the supported media types for the specified types.</returns>
        public IReadOnlyCollection<MediaTypeWithQualityHeaderValue> GetSupportedMediaTypes(Type? modelType, Type? errorType)
        {
            if (errorType is null)
                return GetSupportedMediaTypes(modelType);
            if (modelType is null)
                return GetSupportedMediaTypes(errorType);

            return _mediaTypes2Cache.GetOrAdd((modelType, errorType), _ =>
            {
                var mediaTypes = new HashSet<MediaTypeWithQualityHeaderValue>(GetSupportedMediaTypes(modelType));
                mediaTypes.UnionWith(GetSupportedMediaTypes(errorType));
                return mediaTypes;
            });
        }

        /// <summary>
        /// Adds an <see cref="IHttpContentDeserializer"/> to the collection if an instance of the same type doesn't already exist.
        /// </summary>
        /// <param name="deserializer">The <see cref="IHttpContentDeserializer"/> to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="deserializer"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if an instance of the same type already exists in the collection.</exception>
        public void Add(IHttpContentDeserializer deserializer)
        {
            if (deserializer == null)
                throw new ArgumentNullException(nameof(deserializer));

            var deserializerType = deserializer.GetType();
            if (_collection.Any(item => item.GetType() == deserializerType))
                throw new ArgumentException($"An instance of type {deserializerType.Name} already exists in the collection.", nameof(deserializer));

            _collection.Add(deserializer);
            InvalidateCaches();
        }

        /// <summary>
        /// Removes the first occurrence of a specific <see cref="IHttpContentDeserializer"/> from the collection.
        /// </summary>
        /// <param name="deserializer">The <see cref="IHttpContentDeserializer"/> to remove from the collection.</param>
        /// <returns><c>true</c> if <paramref name="deserializer"/> was successfully removed from the collection; otherwise, <c>false</c>.</returns>
        public bool Remove(IHttpContentDeserializer deserializer)
        {
            if (_collection.Remove(deserializer))
            {
                InvalidateCaches();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the collection contains a specific <see cref="IHttpContentDeserializer"/>.
        /// </summary>
        /// <param name="deserializer">The <see cref="IHttpContentDeserializer"/> to locate in the collection.</param>
        /// <returns><c>true</c> if <paramref name="deserializer"/> is found in the collection; otherwise, <c>false</c>.</returns>
        public bool Contains(IHttpContentDeserializer deserializer)
        {
            return _collection.Contains(deserializer);
        }

        /// <summary>
        /// Finds an <see cref="IHttpContentDeserializer"/> by its type.
        /// </summary>
        /// <typeparam name="T">The type of the deserializer to find.</typeparam>
        /// <returns>The instance of <see cref="IHttpContentDeserializer"/> of the specified type, or <c>null</c> if not found.</returns>
        public T Find<T>() where T : IHttpContentDeserializer
        {
            return (T)_collection.FirstOrDefault(deserializer => deserializer.GetType() == typeof(T));
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            _collection.Clear();
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(IHttpContentDeserializer[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="IEnumerator{T}"/> for <see cref="IHttpContentDeserializer"/>.</returns>
        public IEnumerator<IHttpContentDeserializer> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Resets the caches.
        /// </summary>
        private void InvalidateCaches()
        {
            _deserializersCache.Clear();
            _mediaTypes1Cache.Clear();
            _mediaTypes2Cache.Clear();
        }

        /// <summary>
        /// Converts a media type string to a <see cref="MediaTypeWithQualityHeaderValue"/> instance.
        /// </summary>
        /// <param name="mediaType">The media type string to convert.</param>
        /// <returns>A <see cref="MediaTypeWithQualityHeaderValue"/> instance corresponding to the specified media type string.</returns>
        /// <remarks>
        /// This method utilizes a concurrent dictionary to cache and reuse <see cref="MediaTypeWithQualityHeaderValue"/> instances based on media type 
        /// strings. It ensures that for each unique media type string, only one <see cref="MediaTypeWithQualityHeaderValue"/> instance is created and 
        /// returned, optimizing memory usage and improving performance by avoiding unnecessary allocations.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MediaTypeWithQualityHeaderValue MediaTypeHeaderValue(string mediaType)
        {
            return _mediaTypeHeaderValues.GetOrAdd(mediaType, mt => new MediaTypeWithQualityHeaderValue(mt));
        }
    }
}
