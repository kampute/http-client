// Copyright (C) 2025 Kampute
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
    /// Represents the error handlers of an <see cref="HttpRestClient"/>, in the order they were added.
    /// </summary>
    /// <remarks>
    /// The client asks the handlers that <see cref="GetHandlersFor"/> returns for the status code of an error response, in order, until one of them
    /// retries. A handler can be added only once.
    /// </remarks>
    public sealed class HttpErrorHandlerCollection : ICollection<IHttpErrorHandler>
    {
        private readonly List<IHttpErrorHandler> _collection = [];

        /// <summary>
        /// Gets the number of handlers in the collection.
        /// </summary>
        /// <value>
        /// The number of handlers in the collection.
        /// </value>
        public int Count => _collection.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        /// <value>
        /// Always <see langword="false"/>.
        /// </value>
        bool ICollection<IHttpErrorHandler>.IsReadOnly => false;

        /// <summary>
        /// Returns the handlers that can handle the specified status code, in the order they were added.
        /// </summary>
        /// <param name="statusCode">The status code of the error response.</param>
        /// <returns>The handlers whose <see cref="IHttpErrorHandler.CanHandle"/> accepts <paramref name="statusCode"/>.</returns>
        public IEnumerable<IHttpErrorHandler> GetHandlersFor(HttpStatusCode statusCode)
        {
            return _collection.Where(errorHandler => errorHandler.CanHandle(statusCode));
        }

        /// <summary>
        /// Adds an <see cref="IHttpErrorHandler"/> to the collection.
        /// </summary>
        /// <param name="errorHandler">The <see cref="IHttpErrorHandler"/> to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorHandler"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="errorHandler"/> is already in the collection.</exception>
        public void Add(IHttpErrorHandler errorHandler)
        {
            if (errorHandler is null)
                throw new ArgumentNullException(nameof(errorHandler));
            if (_collection.Contains(errorHandler))
                throw new ArgumentException("The specified error handler is already in the collection.", nameof(errorHandler));

            _collection.Add(errorHandler);
        }

        /// <summary>
        /// Removes a handler from the collection.
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
