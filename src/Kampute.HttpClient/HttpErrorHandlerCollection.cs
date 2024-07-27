// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Represents a specialized collection of <see cref="IHttpErrorHandler"/> instances.
    /// </summary>
    /// <remarks>
    /// This collection enables the management of <see cref="IHttpErrorHandler"/> instances for handling HTTP errors, 
    /// with capabilities such as adding, removing, and querying error handlers based on HTTP status codes. 
    /// </remarks>
    public sealed class HttpErrorHandlerCollection : ICollection<IHttpErrorHandler>
    {
        private readonly List<IHttpErrorHandler> _collection = [];

        /// <summary>
        /// Gets the number of <see cref="IHttpErrorHandler"/> instances contained in the collection.
        /// </summary>
        /// <value>
        /// The number of <see cref="IHttpErrorHandler"/> instances contained in the collection.
        /// </value>
        public int Count => _collection.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only. Always returns <see langword="false"/> for this implementation.
        /// </summary>
        /// <value>
        /// Indicates whether the collection is read-only. This property always returns <see langword="false"/>.
        /// </value>
        bool ICollection<IHttpErrorHandler>.IsReadOnly => false;

        /// <summary>
        /// Retrieves all <see cref="IHttpErrorHandler"/> instances in the collection that support handling a specific HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to query.</param>
        /// <returns>An enumerable of <see cref="IHttpErrorHandler"/> that can handle the specified status code.</returns>
        public IEnumerable<IHttpErrorHandler> GetHandlersFor(HttpStatusCode statusCode)
        {
            return _collection.Where(errorHandler => errorHandler.CanHandle(statusCode));
        }

        /// <summary>
        /// Adds an <see cref="IHttpErrorHandler"/> to the collection.
        /// </summary>
        /// <param name="errorHandler">The <see cref="IHttpErrorHandler"/> to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorHandler"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="errorHandler"/> is already present in the collection, as duplicates are not allowed.</exception>
        public void Add(IHttpErrorHandler errorHandler)
        {
            if (errorHandler is null)
                throw new ArgumentNullException(nameof(errorHandler));
            if (_collection.Contains(errorHandler))
                throw new ArgumentException("The specified error handler is already in the collection.", nameof(errorHandler));

            _collection.Add(errorHandler);
        }

        /// <summary>
        /// Removes the first occurrence of a specific <see cref="IHttpErrorHandler"/> from the collection.
        /// </summary>
        /// <param name="errorHandler">The <see cref="IHttpErrorHandler"/> to remove from the collection.</param>
        /// <returns><see langword="true"/> if <paramref name="errorHandler"/> was successfully removed from the collection; otherwise, <see langword="false"/>.</returns>
        public bool Remove(IHttpErrorHandler errorHandler)
        {
            return _collection.Remove(errorHandler);
        }

        /// <summary>
        /// Determines whether the collection contains a specific <see cref="IHttpErrorHandler"/>.
        /// </summary>
        /// <param name="errorHandler">The <see cref="IHttpErrorHandler"/> to locate in the collection.</param>
        /// <returns><see langword="true"/> if <paramref name="errorHandler"/> is found in the collection; otherwise, <see langword="false"/>.</returns>
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
    }
}
