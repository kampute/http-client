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
    using System.Net;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a specialized collection of <see cref="IHttpErrorHandler"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This collection enables the management of <see cref="IHttpErrorHandler"/> instances for handling HTTP errors, 
    /// with capabilities such as adding, removing, and querying error handlers based on HTTP status codes. It leverages 
    /// an internal caching mechanism to optimize the retrieval of error handlers for specific HTTP status codes. 
    /// </para>
    /// <para>
    /// This cache is dynamically updated to reflect changes in the collection, ensuring that queries for error handlers 
    /// are performed efficiently. The use of caching minimizes the need to repeatedly evaluate which handlers are applicable 
    /// for a given status code, thereby enhancing performance, especially in scenarios where error handling is a frequent 
    /// operation.
    /// </para>
    /// </remarks>
    public sealed class HttpErrorHandlerCollection : ICollection<IHttpErrorHandler>
    {
        private readonly List<IHttpErrorHandler> _collection = [];
        private readonly ConcurrentDictionary<HttpStatusCode, IReadOnlyCollection<IHttpErrorHandler>> _cache = new();

        /// <summary>
        /// Gets the number of <see cref="IHttpErrorHandler"/> instances contained in the collection.
        /// </summary>
        /// <value>
        /// The number of <see cref="IHttpErrorHandler"/> instances contained in the collection.
        /// </value>
        public int Count => _collection.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only. Always returns <c>false</c> for this implementation.
        /// </summary>
        /// <value>
        /// Indicates whether the collection is read-only. This property always returns <c>false</c>.
        /// </value>
        bool ICollection<IHttpErrorHandler>.IsReadOnly => false;

        /// <summary>
        /// Retrieves all <see cref="IHttpErrorHandler"/> instances in the collection that support handling a specific HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to query.</param>
        /// <returns>A read-only collection of <see cref="IHttpErrorHandler"/> that can handle the specified status code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyCollection<IHttpErrorHandler> For(HttpStatusCode statusCode)
        {
            return _cache.GetOrAdd(statusCode, _ => _collection.Where(errorHandler => errorHandler.CanHandle(statusCode)).ToArray());
        }

        /// <summary>
        /// Adds an <see cref="IHttpErrorHandler"/> to the collection.
        /// </summary>
        /// <param name="errorHandler">The <see cref="IHttpErrorHandler"/> to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorHandler"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="errorHandler"/> is already present in the collection, as duplicates are not allowed.</exception>
        public void Add(IHttpErrorHandler errorHandler)
        {
            if (errorHandler == null)
                throw new ArgumentNullException(nameof(errorHandler));
            if (_collection.Contains(errorHandler))
                throw new ArgumentException("A duplicate error handler cannot be added to the collection.", nameof(errorHandler));

            _collection.Add(errorHandler);
            UpdateCacheForErrorHandler(errorHandler);
        }

        /// <summary>
        /// Removes the first occurrence of a specific <see cref="IHttpErrorHandler"/> from the collection.
        /// </summary>
        /// <param name="errorHandler">The <see cref="IHttpErrorHandler"/> to remove from the collection.</param>
        /// <returns><c>true</c> if <paramref name="errorHandler"/> was successfully removed from the collection; otherwise, <c>false</c>.</returns>
        public bool Remove(IHttpErrorHandler errorHandler)
        {
            if (_collection.Remove(errorHandler))
            {
                UpdateCacheForErrorHandler(errorHandler);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the collection contains a specific <see cref="IHttpErrorHandler"/>.
        /// </summary>
        /// <param name="errorHandler">The <see cref="IHttpErrorHandler"/> to locate in the collection.</param>
        /// <returns><c>true</c> if <paramref name="errorHandler"/> is found in the collection; otherwise, <c>false</c>.</returns>
        public bool Contains(IHttpErrorHandler errorHandler)
        {
            return _collection.Contains(errorHandler);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            _collection.Clear();
            _cache.Clear();
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        void ICollection<IHttpErrorHandler>.CopyTo(IHttpErrorHandler[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="IEnumerator{T}"/> for <see cref="IHttpErrorHandler"/>.</returns>
        public IEnumerator<IHttpErrorHandler> GetEnumerator()
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
        /// Updates the cache entries related to a specific <see cref="IHttpErrorHandler"/>.
        /// </summary>
        /// <param name="errorHandler">The <see cref="IHttpErrorHandler"/> for which the cache should be updated.</param>
        /// <remarks>
        /// This method removes the cache entries for HTTP status codes that the given <paramref name="errorHandler"/> can handle.
        /// This ensures that the cache stays up-to-date when error handlers are added or removed from the collection.
        /// </remarks>
        private void UpdateCacheForErrorHandler(IHttpErrorHandler errorHandler)
        {
            if (_cache.Count == 0)
                return;

            var invalidatedCacheKeys = _cache.Keys.Where(errorHandler.CanHandle).ToList();
            if (invalidatedCacheKeys.Count == 0)
                return;

            foreach (var statusCode in invalidatedCacheKeys)
                _cache.TryRemove(statusCode, out _);
        }
    }
}
