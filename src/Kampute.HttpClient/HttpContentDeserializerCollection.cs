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
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a specialized collection of <see cref="IHttpContentDeserializer"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This collection provides capabilities for managing <see cref="IHttpContentDeserializer"/> instances, including adding, removing,
    /// and selecting deserializers based on media types and model types. It leverages internal caches to optimize performance for frequently
    /// accessed deserializers, significantly enhancing efficiency in scenarios where media types and model types are repeatedly queried.
    /// </para>
    /// <para>
    /// Caches within the collection are automatically invalidated and updated upon modification of the deserializer inventory, ensuring that access
    /// patterns remain efficient and that overhead associated with dynamic updates is minimized.
    /// </para>
    /// <para>
    /// The collection is designed to be flexible and adaptable, accommodating a wide range of models, media types, and server behaviors without
    /// prior knowledge of specific implementations. It supports assigning quality factors to media types, prioritizing those that can deserialize
    /// both model and error types (q=1.0) over those that solely support error types (q=0.9). This nuanced handling of media types facilitates
    /// sophisticated content negotiation strategies, ensuring clients can effectively communicate preferences for both successful responses and
    /// error scenarios.
    /// </para>
    /// </remarks>
    public sealed class HttpContentDeserializerCollection : ICollection<IHttpContentDeserializer>
    {
        private static readonly string[] AllMediaTypes = ["*/*"];

        private readonly List<IHttpContentDeserializer> _collection = [];
        private readonly ConcurrentDictionary<Type, IReadOnlyCollection<string>> _mediaTypes1Cache = new();
        private readonly ConcurrentDictionary<(Type, Type), IReadOnlyCollection<string>> _mediaTypes2Cache = new();

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
        /// Retrieves the first <see cref="IHttpContentDeserializer"/> instances in the collection that support deserializing a specific media type and model type.
        /// </summary>
        /// <param name="mediaType">The media type to deserialize.</param>
        /// <param name="modelType">The type of the model to deserialize.</param>
        /// <returns>An instance of <see cref="IHttpContentDeserializer"/> that can deserialize the specified media type and model type, or <c>null</c> if none is found.</returns>
        public IHttpContentDeserializer? GetDeserializerFor(string mediaType, Type modelType)
        {
            foreach (var deserializer in _collection)
                if (deserializer.CanDeserialize(mediaType, modelType))
                    return deserializer;

            return null;
        }

        /// <summary>
        /// Retrieves all supported media types for a specified model type from the collection of deserializers.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <returns>A read-only collection of strings that represent the media types supported for deserializing the specified model type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<string> GetAcceptableMediaTypes(Type? modelType)
        {
            if (modelType is null)
                return AllMediaTypes;

            return _collection.Count switch
            {
                0 => [],
                1 => _collection[0].GetSupportedMediaTypes(modelType),
                _ => _mediaTypes1Cache.GetOrAdd(modelType, CollectSupportedMediaTypes)
            };
        }

        /// <summary>
        /// Retrieves all supported media types for a specified model type and error type from the collection of deserializers.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <param name="errorType">The type of the error for which to retrieve supported media types.</param>
        /// <returns>A read-only collection of strings representing the supported media types for the specified types.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<string> GetAcceptableMediaTypes(Type? modelType, Type? errorType)
        {
            if (errorType is null)
                return GetAcceptableMediaTypes(modelType);

            if (modelType is null)
                return GetAcceptableMediaTypes(errorType).Concat(AllMediaTypes);

            return _mediaTypes2Cache.GetOrAdd((modelType, errorType), CollectSupportedMediaTypes);
        }

        /// <summary>
        /// Adds an <see cref="IHttpContentDeserializer"/> to the collection if an instance of the same type doesn't already exist.
        /// </summary>
        /// <param name="deserializer">The <see cref="IHttpContentDeserializer"/> to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="deserializer"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if an instance of the same type already exists in the collection.</exception>
        public void Add(IHttpContentDeserializer deserializer)
        {
            if (deserializer is null)
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
            InvalidateCaches();
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        void ICollection<IHttpContentDeserializer>.CopyTo(IHttpContentDeserializer[] array, int arrayIndex)
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
            _mediaTypes1Cache.Clear();
            _mediaTypes2Cache.Clear();
        }

        /// <summary>
        /// Retrieves all supported media types for a specified model type from the collection of deserializers.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media type header values.</param>
        /// <returns>A read-only collection of strings that represent the media types supported for deserializing the specified model type.</returns>
        private IReadOnlyCollection<string> CollectSupportedMediaTypes(Type modelType)
        {
            var uniqueMediaTypes = new HashSet<string>();
            var orderedMediaTypes = new List<string>();
            foreach (var deserializer in _collection)
            {
                foreach (var mediaType in deserializer.GetSupportedMediaTypes(modelType))
                {
                    if (uniqueMediaTypes.Add(mediaType))
                        orderedMediaTypes.Add(mediaType);
                }
            }
            orderedMediaTypes.TrimExcess();
            return orderedMediaTypes;
        }

        /// <summary>
        /// Retrieves all supported media types for a specified pair of model and error types from the collection of deserializers.
        /// </summary>
        /// <param name="types">
        /// A tuple containing two types used to collect and aggregate media types that can deserialize objects of these types from HTTP content:
        /// <list type="bullet">
        ///   <item>
        ///     <term>Item1</term>
        ///     <description>The type of the model for which to retrieve supported media types.</description>
        ///   </item>
        ///   <item>
        ///     <term>Item2</term>
        ///     <description>The type of the error for which to retrieve supported media types.</description>
        ///   </item>
        /// </list>
        /// </param>
        /// <returns>
        /// A read-only collection of strings that represent the media types supported for deserializing the specified types.
        /// </returns>
        private IReadOnlyCollection<string> CollectSupportedMediaTypes((Type, Type) types)
        {
            var uniqueMediaTypes = new HashSet<string>();
            var orderedMediaTypes = new List<string>();

            // Add media type header values supporting model
            foreach (var mediaType in GetAcceptableMediaTypes(types.Item1))
            {
                if (uniqueMediaTypes.Add(mediaType))
                    orderedMediaTypes.Add(mediaType);
            }

            // Add media type header values supporting error
            foreach (var mediaType in GetAcceptableMediaTypes(types.Item2))
            {
                if (uniqueMediaTypes.Add(mediaType))
                    orderedMediaTypes.Add(mediaType);
            }

            orderedMediaTypes.TrimExcess();
            return orderedMediaTypes;
        }
    }
}
