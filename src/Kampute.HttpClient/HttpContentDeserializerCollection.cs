// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.Utilities;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Represents a specialized collection of <see cref="IHttpContentDeserializer"/> instances.
    /// </summary>
    /// <remarks>
    /// This collection provides capabilities for managing <see cref="IHttpContentDeserializer"/> instances, including adding, removing,
    /// and selecting deserializers based on media types and model types. It leverages internal caches to optimize performance for frequently
    /// accessed deserializers, significantly enhancing efficiency in scenarios where media types and model types are repeatedly queried.
    /// </remarks>
    public sealed class HttpContentDeserializerCollection : ICollection<IHttpContentDeserializer>, IReadOnlyCollection<IHttpContentDeserializer>
    {
        private static readonly string[] AllMediaTypes = ["*/*"];

        private readonly List<IHttpContentDeserializer> _collection;
        private readonly Lazy<AcceptableMediaTypeCache> _acceptCache;
        private readonly FlyweightCache<(string, Type), IHttpContentDeserializer?> _deserializerCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentDeserializerCollection"/> class.
        /// </summary>
        public HttpContentDeserializerCollection()
        {
            _collection = [];
            _deserializerCache = new(key => FindDeserializer(key.Item1, key.Item2));
            _acceptCache = new(() => new(this), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Gets the number of <see cref="IHttpContentDeserializer"/> instances contained in the collection.
        /// </summary>
        /// <value>
        /// The number of <see cref="IHttpContentDeserializer"/> instances contained in the collection.
        /// </value>
        public int Count => _collection.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only. Always returns <see langword="false"/> for this implementation.
        /// </summary>
        /// <value>
        /// Indicates whether the collection is read-only. This implementation always returns <see langword="false"/>.
        /// </value> 
        bool ICollection<IHttpContentDeserializer>.IsReadOnly => false;

        /// <summary>
        /// Retrieves the first <see cref="IHttpContentDeserializer"/> instances in the collection that support deserializing a specific media type and model type.
        /// </summary>
        /// <param name="mediaType">The media type to deserialize.</param>
        /// <param name="modelType">The type of the model to deserialize.</param>
        /// <returns>An instance of <see cref="IHttpContentDeserializer"/> that can deserialize the specified media type and model type, or <see langword="null"/> if none is found.</returns>
        public IHttpContentDeserializer? GetDeserializerFor(string mediaType, Type modelType)
        {
            return _deserializerCache.Get((mediaType, modelType));
        }

        /// <summary>
        /// Retrieves all supported media types for a specified model type from the collection of deserializers.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <returns>An enumerable of strings that represent the media types supported for deserializing the specified model type.</returns>
        /// <remarks>
        /// <para>
        /// This method determines the supported media types for deserializing content based on the given model type.
        /// </para>
        /// <para>
        /// If the model type is <see langword="null"/>, this method returns a collection containing only <c>"*/*"</c>, signifying that all media types are acceptable.
        /// </para>
        /// <para>
        /// For non-null model types, the method aggregates media types supported by the registered deserializers for that specific model type.
        /// </para>
        /// </remarks>
        public IEnumerable<string> GetAcceptableMediaTypes(Type? modelType)
        {
            if (modelType is null)
                return AllMediaTypes;

            return _collection.Count switch
            {
                0 => [],
                1 => _collection[0].GetSupportedMediaTypes(modelType),
                _ => _acceptCache.Value.GetSupportedMediaTypes(modelType)
            };
        }

        /// <summary>
        /// Retrieves all supported media types for a specified model type and error type from the collection of deserializers.
        /// </summary>
        /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
        /// <param name="errorType">The type of the error for which to retrieve supported media types.</param>
        /// <returns>An enumerable of strings representing the supported media types for the specified types.</returns>
        /// <remarks>
        /// <para>
        /// This method determines the supported media types for deserializing content based on the given <paramref name="modelType"/> and <paramref name="errorType"/>.
        /// </para>
        /// <para>
        /// If both <paramref name="modelType"/> and <paramref name="errorType"/> are provided, it aggregates and returns the media types that support deserializing either type.
        /// </para>
        /// <para>
        /// If only <paramref name="modelType"/> is provided and <paramref name="errorType"/> is <see langword="null"/>, the result includes media types exclusively supporting the <paramref name="modelType"/>.
        /// </para>
        /// <para>
        /// Conversely, if <paramref name="modelType"/> is <see langword="null"/> and <paramref name="errorType"/> is provided, the result includes media types supporting the <paramref name="errorType"/>,
        /// augmented by <c>"*/*"</c> to indicate that all media types are acceptable for the <paramref name="modelType"/>.
        /// </para>
        /// <para>
        /// If both parameters are <see langword="null"/>, the method defaults to returning <c>"*/*"</c> only, implying general acceptability of any media type.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<string> GetAcceptableMediaTypes(Type? modelType, Type? errorType)
        {
            if (errorType is null)
                return GetAcceptableMediaTypes(modelType);

            if (modelType is null)
                return GetAcceptableMediaTypes(errorType).Concat(AllMediaTypes);

            return _acceptCache.Value.GetSupportedMediaTypes(modelType, errorType);
        }

        /// <summary>
        /// Adds an <see cref="IHttpContentDeserializer"/> to the collection if an instance of the same type doesn't already exist.
        /// </summary>
        /// <param name="deserializer">The <see cref="IHttpContentDeserializer"/> to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="deserializer"/> is <see langword="null"/>.</exception>
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
        /// <returns><see langword="true"/> if <paramref name="deserializer"/> was successfully removed from the collection; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/> if <paramref name="deserializer"/> is found in the collection; otherwise, <see langword="false"/>.</returns>
        public bool Contains(IHttpContentDeserializer deserializer)
        {
            return _collection.Contains(deserializer);
        }

        /// <summary>
        /// Finds an <see cref="IHttpContentDeserializer"/> by its type.
        /// </summary>
        /// <typeparam name="T">The type of the deserializer to find.</typeparam>
        /// <returns>The instance of <see cref="IHttpContentDeserializer"/> of the specified type, or <see langword="null"/> if not found.</returns>
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
        /// Locates the first <see cref="IHttpContentDeserializer"/> instances in the collection that support deserializing a specific media type and model type.
        /// </summary>
        /// <param name="mediaType">The media type to deserialize.</param>
        /// <param name="modelType">The type of the model to deserialize.</param>
        /// <returns>An instance of <see cref="IHttpContentDeserializer"/> that can deserialize the specified media type and model type, or <see langword="null"/> if none is found.</returns>
        private IHttpContentDeserializer? FindDeserializer(string mediaType, Type modelType)
        {
            foreach (var deserializer in _collection)
                if (deserializer.CanDeserialize(mediaType, modelType))
                    return deserializer;

            return null;
        }

        /// <summary>
        /// Resets the caches.
        /// </summary>
        private void InvalidateCaches()
        {
            _deserializerCache.Clear();
            if (_acceptCache.IsValueCreated)
                _acceptCache.Value.Clear();
        }

        #region Helper Types

        /// <summary>
        /// Provides cache of supported media types for .NET object types.
        /// </summary>
        private sealed class AcceptableMediaTypeCache
        {
            private readonly IReadOnlyCollection<IHttpContentDeserializer> _deserializers;
            private readonly FlyweightCache<Type, IReadOnlyCollection<string>> _singles;
            private readonly FlyweightCache<(Type, Type), IReadOnlyCollection<string>> _duals;

            public AcceptableMediaTypeCache(IReadOnlyCollection<IHttpContentDeserializer> deserializers)
            {
                _deserializers = deserializers;
                _singles = new(CollectSupportedMediaTypes);
                _duals = new(CollectSupportedMediaTypes);
            }

            /// <summary>
            /// Retrieves all supported media types for a specified model type from the collection of deserializers.
            /// </summary>
            /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
            /// <returns>A read-only collection of strings that represent the media types supported for deserializing the specified model type.</returns>
            public IReadOnlyCollection<string> GetSupportedMediaTypes(Type modelType)
            {
                return _singles.Get(modelType);
            }

            /// <summary>
            /// Retrieves all supported media types for a specified model type and error type from the collection of deserializers.
            /// </summary>
            /// <param name="modelType">The type of the model for which to retrieve supported media types.</param>
            /// <param name="errorType">The type of the error for which to retrieve supported media types.</param>
            /// <returns>A read-only collection of strings representing the supported media types for the specified types.</returns>
            public IReadOnlyCollection<string> GetSupportedMediaTypes(Type modelType, Type errorType)
            {
                return _duals.Get((modelType, errorType));
            }

            /// <summary>
            /// Clears the cache.
            /// </summary>
            public void Clear()
            {
                _singles.Clear();
                _duals.Clear();
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

                foreach (var deserializer in _deserializers)
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
                var (modelType, errorType) = types;
                var uniqueMediaTypes = new HashSet<string>();
                var orderedMediaTypes = new List<string>();

                foreach (var deserializer in _deserializers)
                {
                    foreach (var mediaType in deserializer.GetSupportedMediaTypes(modelType))
                    {
                        if (uniqueMediaTypes.Add(mediaType))
                            orderedMediaTypes.Add(mediaType);
                    }
                }

                foreach (var deserializer in _deserializers)
                {
                    foreach (var mediaType in deserializer.GetSupportedMediaTypes(errorType))
                    {
                        if (uniqueMediaTypes.Add(mediaType))
                            orderedMediaTypes.Add(mediaType);
                    }
                }

                orderedMediaTypes.TrimExcess();
                return orderedMediaTypes;
            }
        }

        #endregion
    }
}
