// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.Utilities;
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Represents the content formatters of an <see cref="HttpRestClient"/>, in the order they were added.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This collection holds the content formatters of an <see cref="HttpRestClient"/>. It selects the formatter that reads a response with
    /// <see cref="GetReaderFor"/>, the formatter that writes a request payload with <see cref="GetWriterFor"/>, and the media types of the
    /// <c>Accept</c> header with <see cref="GetAcceptableMediaTypes(Type?)"/>. When several formatters match, the one added first is used.
    /// </para>
    /// <para>
    /// Media types that differ only in case are treated as the same media type. A formatter that is found is cached for later lookups of the
    /// same media type and type; a failed lookup is not cached.
    /// </para>
    /// </remarks>
    public sealed class HttpContentFormatterCollection : ICollection<IHttpContentFormatter>, IReadOnlyCollection<IHttpContentFormatter>
    {
        private static readonly string[] AllMediaTypes = ["*/*"];

        private readonly List<IHttpContentFormatter> _collection;
        private readonly Lazy<AcceptableMediaTypeCache> _acceptCache;
        private readonly ConcurrentDictionary<(string, Type), IHttpContentFormatter> _readerCache;
        private readonly ConcurrentDictionary<(string, Type), IHttpContentFormatter> _writerCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentFormatterCollection"/> class.
        /// </summary>
        public HttpContentFormatterCollection()
        {
            _collection = [];
            _readerCache = new(MediaTypeAndObjectTypeComparer.Instance);
            _writerCache = new(MediaTypeAndObjectTypeComparer.Instance);
            _acceptCache = new(() => new(this), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Gets the number of formatters in the collection.
        /// </summary>
        /// <value>
        /// The number of formatters in the collection.
        /// </value>
        public int Count => _collection.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        /// <value>
        /// Always <see langword="false"/>.
        /// </value>
        bool ICollection<IHttpContentFormatter>.IsReadOnly => false;

        /// <summary>
        /// Retrieves the first formatter in the collection that can read content of a specific media type into a specific model type.
        /// </summary>
        /// <param name="mediaType">The media type of the content.</param>
        /// <param name="modelType">The type of the object to read.</param>
        /// <returns>The first <see cref="IHttpContentFormatter"/> whose <see cref="IHttpContentFormatter.CanRead"/> returns <see langword="true"/>, or <see langword="null"/> if there is none.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="mediaType"/> or <paramref name="modelType"/> is <see langword="null"/>.</exception>
        public IHttpContentFormatter? GetReaderFor(string mediaType, Type modelType)
        {
            if (mediaType is null)
                throw new ArgumentNullException(nameof(mediaType));
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            return FindCached(_readerCache, mediaType, modelType, static (formatter, mediaType, type) => formatter.CanRead(mediaType, type));
        }

        /// <summary>
        /// Retrieves the first formatter in the collection that can write a payload of a specific type in a specific media type.
        /// </summary>
        /// <param name="mediaType">The media type of the content to create.</param>
        /// <param name="payloadType">The type of the payload to write.</param>
        /// <returns>The first <see cref="IHttpContentFormatter"/> whose <see cref="IHttpContentFormatter.CanWrite"/> returns <see langword="true"/>, or <see langword="null"/> if there is none.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="mediaType"/> or <paramref name="payloadType"/> is <see langword="null"/>.</exception>
        public IHttpContentFormatter? GetWriterFor(string mediaType, Type payloadType)
        {
            if (mediaType is null)
                throw new ArgumentNullException(nameof(mediaType));
            if (payloadType is null)
                throw new ArgumentNullException(nameof(payloadType));

            return FindCached(_writerCache, mediaType, payloadType, static (formatter, mediaType, type) => formatter.CanWrite(mediaType, type));
        }

        /// <summary>
        /// Retrieves the media types that the formatters in the collection can read into a specified model type.
        /// </summary>
        /// <param name="modelType">The type of the object to read, or <see langword="null"/> if any content is acceptable.</param>
        /// <returns>The media types that can be read into <paramref name="modelType"/>, in the order of the formatters, without duplicates.</returns>
        /// <remarks>
        /// <para>
        /// If <paramref name="modelType"/> is <see langword="null"/>, this method returns only <c>"*/*"</c>, signifying that all media types are acceptable.
        /// </para>
        /// <para>
        /// Send-only formatters contribute no media types.
        /// </para>
        /// </remarks>
        public IEnumerable<string> GetAcceptableMediaTypes(Type? modelType)
        {
            if (modelType is null)
                return AllMediaTypes;

            return _collection.Count switch
            {
                0 => [],
                1 => _collection[0].GetReadableMediaTypes(modelType),
                _ => _acceptCache.Value.GetReadableMediaTypes(modelType)
            };
        }

        /// <summary>
        /// Retrieves the media types that the formatters in the collection can read into a specified model type or error type.
        /// </summary>
        /// <param name="modelType">The type of the object to read, or <see langword="null"/> if any content is acceptable.</param>
        /// <param name="errorType">The type of the error object to read, or <see langword="null"/> if errors are not read.</param>
        /// <returns>The media types that can be read into either type, model type first, without duplicates.</returns>
        /// <remarks>
        /// <para>
        /// If <paramref name="errorType"/> is <see langword="null"/>, the result is the same as for <see cref="GetAcceptableMediaTypes(Type?)"/>.
        /// </para>
        /// <para>
        /// If <paramref name="modelType"/> is <see langword="null"/> and <paramref name="errorType"/> is provided, the result includes the media types
        /// that can be read into <paramref name="errorType"/>, followed by <c>"*/*"</c> to indicate that any content is acceptable for the model.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<string> GetAcceptableMediaTypes(Type? modelType, Type? errorType)
        {
            if (errorType is null)
                return GetAcceptableMediaTypes(modelType);

            if (modelType is null)
                return GetAcceptableMediaTypes(errorType).Concat(AllMediaTypes);

            return _acceptCache.Value.GetReadableMediaTypes(modelType, errorType);
        }

        /// <summary>
        /// Adds a formatter to the collection, which can hold one formatter of each type.
        /// </summary>
        /// <param name="formatter">The <see cref="IHttpContentFormatter"/> to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="formatter"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if an instance of the same type already exists in the collection.</exception>
        public void Add(IHttpContentFormatter formatter)
        {
            if (formatter is null)
                throw new ArgumentNullException(nameof(formatter));

            var formatterType = formatter.GetType();
            if (_collection.Any(item => item.GetType() == formatterType))
                throw new ArgumentException($"An instance of type {formatterType.Name} already exists in the collection.", nameof(formatter));

            _collection.Add(formatter);
            InvalidateCaches();
        }

        /// <summary>
        /// Removes a formatter from the collection.
        /// </summary>
        /// <param name="formatter">The <see cref="IHttpContentFormatter"/> to remove from the collection.</param>
        /// <returns><see langword="true"/> if <paramref name="formatter"/> was successfully removed from the collection; otherwise, <see langword="false"/>.</returns>
        public bool Remove(IHttpContentFormatter formatter)
        {
            if (_collection.Remove(formatter))
            {
                InvalidateCaches();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the collection contains a specific <see cref="IHttpContentFormatter"/>.
        /// </summary>
        /// <param name="formatter">The <see cref="IHttpContentFormatter"/> to locate in the collection.</param>
        /// <returns><see langword="true"/> if <paramref name="formatter"/> is found in the collection; otherwise, <see langword="false"/>.</returns>
        public bool Contains(IHttpContentFormatter formatter)
        {
            return _collection.Contains(formatter);
        }

        /// <summary>
        /// Finds the formatter of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the formatter to find.</typeparam>
        /// <returns>The formatter whose type is exactly <typeparamref name="T"/>, or <see langword="null"/> if the collection has none.</returns>
        /// <remarks>
        /// A formatter of a type derived from <typeparamref name="T"/> does not match.
        /// </remarks>
        public T? Find<T>() where T : IHttpContentFormatter
        {
            return (T?)_collection.FirstOrDefault(formatter => formatter.GetType() == typeof(T));
        }

        /// <summary>
        /// Finds the formatter of the specified type, or creates one with default options if the collection has none.
        /// </summary>
        /// <typeparam name="T">The type of the formatter to find.</typeparam>
        /// <returns>The formatter whose type is exactly <typeparamref name="T"/>, or a new instance of <typeparamref name="T"/> that is not added to the collection.</returns>
        /// <remarks>
        /// A new instance is created on each call that finds no formatter, rather than shared, so that changing its options cannot affect other clients.
        /// </remarks>
        public T FindOrDefault<T>() where T : IHttpContentFormatter, new()
        {
            return Find<T>() ?? new T();
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
        void ICollection<IHttpContentFormatter>.CopyTo(IHttpContentFormatter[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="IEnumerator{T}"/> for <see cref="IHttpContentFormatter"/>.</returns>
        public IEnumerator<IHttpContentFormatter> GetEnumerator()
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
        /// Returns the cached formatter for a media type and type, or finds the first matching formatter and caches it.
        /// </summary>
        /// <param name="cache">The cache of the lookup.</param>
        /// <param name="mediaType">The media type to match.</param>
        /// <param name="type">The type of the object to read or write.</param>
        /// <param name="matches">The function that tells whether a formatter matches.</param>
        /// <returns>The first matching formatter, or <see langword="null"/> if there is none.</returns>
        private IHttpContentFormatter? FindCached
        (
            ConcurrentDictionary<(string, Type), IHttpContentFormatter> cache,
            string mediaType,
            Type type,
            Func<IHttpContentFormatter, string, Type, bool> matches
        )
        {
            var key = (mediaType, type);
            if (cache.TryGetValue(key, out var formatter))
                return formatter;

            foreach (var candidate in _collection)
            {
                if (matches(candidate, mediaType, type))
                {
                    cache.TryAdd(key, candidate);
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Resets the caches.
        /// </summary>
        private void InvalidateCaches()
        {
            _readerCache.Clear();
            _writerCache.Clear();
            if (_acceptCache.IsValueCreated)
                _acceptCache.Value.Clear();
        }

        #region Helper Types

        /// <summary>
        /// Compares pairs of media type and object type, ignoring the case of the media type.
        /// </summary>
        private sealed class MediaTypeAndObjectTypeComparer : IEqualityComparer<(string, Type)>
        {
            public static readonly MediaTypeAndObjectTypeComparer Instance = new();

            public bool Equals((string, Type) x, (string, Type) y)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1) && x.Item2 == y.Item2;
            }

            public int GetHashCode((string, Type) obj)
            {
                return unchecked(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1) * 31 + obj.Item2.GetHashCode());
            }
        }

        /// <summary>
        /// Provides cache of readable media types for .NET object types.
        /// </summary>
        private sealed class AcceptableMediaTypeCache
        {
            private readonly IReadOnlyCollection<IHttpContentFormatter> _formatters;
            private readonly FlyweightCache<Type, IReadOnlyCollection<string>> _singles;
            private readonly FlyweightCache<(Type, Type), IReadOnlyCollection<string>> _duals;

            public AcceptableMediaTypeCache(IReadOnlyCollection<IHttpContentFormatter> formatters)
            {
                _formatters = formatters;
                _singles = new(modelType => CollectReadableMediaTypes(modelType));
                _duals = new(types => CollectReadableMediaTypes(types.Item1, types.Item2));
            }

            /// <summary>
            /// Retrieves the media types that the formatters can read into a specified model type.
            /// </summary>
            /// <param name="modelType">The type of the object to read.</param>
            /// <returns>A read-only collection of the media types, without duplicates.</returns>
            public IReadOnlyCollection<string> GetReadableMediaTypes(Type modelType)
            {
                return _singles.Get(modelType);
            }

            /// <summary>
            /// Retrieves the media types that the formatters can read into a specified model type or error type.
            /// </summary>
            /// <param name="modelType">The type of the object to read.</param>
            /// <param name="errorType">The type of the error object to read.</param>
            /// <returns>A read-only collection of the media types, model type first, without duplicates.</returns>
            public IReadOnlyCollection<string> GetReadableMediaTypes(Type modelType, Type errorType)
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
            /// Collects the media types that the formatters can read into the specified types, in order and without duplicates.
            /// </summary>
            /// <param name="types">The types of the objects to read.</param>
            /// <returns>A read-only collection of the media types.</returns>
            private IReadOnlyCollection<string> CollectReadableMediaTypes(params Type[] types)
            {
                var uniqueMediaTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var orderedMediaTypes = new List<string>();

                foreach (var type in types)
                {
                    foreach (var formatter in _formatters)
                    {
                        foreach (var mediaType in formatter.GetReadableMediaTypes(type))
                        {
                            if (uniqueMediaTypes.Add(mediaType))
                                orderedMediaTypes.Add(mediaType);
                        }
                    }
                }

                orderedMediaTypes.TrimExcess();
                return orderedMediaTypes;
            }
        }

        #endregion
    }
}
