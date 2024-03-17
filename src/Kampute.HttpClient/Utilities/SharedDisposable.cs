// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Manages shared access to a disposable resource, ensuring it is correctly disposed of when no longer in use.
    /// </summary>
    /// <remarks>
    /// This class is particularly useful for managing resources that are expensive to create and can be safely shared across different parts 
    /// of an application. It ensures that the resource remains alive as long as it is needed and is properly cleaned up afterwards. This pattern
    /// helps prevent resource leaks and promotes efficient resource usage. Additionally, the implementation is thread-safe, making it suitable for
    /// use in multi-threaded environments where resources may be accessed concurrently.
    /// </remarks>
    /// <typeparam name="T">The type of the disposable object. Must be a class that implements <see cref="IDisposable"/>.</typeparam>
    public sealed class SharedDisposable<T> where T : class, IDisposable
    {
        private volatile T? _instance;
        private volatile int _referenceCount = 0;
        private readonly Func<T> _factory;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDisposable{T}"/> class with a factory function.
        /// </summary>
        /// <param name="factory">A function that creates an instance of the object <typeparamref name="T"/> when needed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <c>null</c>.</exception>
        public SharedDisposable(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Gets the current number of active references to the managed disposable object.
        /// </summary>
        /// <value>
        /// The number of active references.
        /// </value>
        public int ReferenceCount => _referenceCount;

        /// <summary>
        /// Acquires a reference to the managed object, creating the object if necessary.
        /// </summary>
        /// <returns>A reference to the managed object.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the factory fails to create an instance of type <typeparamref name="T"/>.</exception>
        public T Acquire()
        {
            lock (_lock)
            {
                if (_referenceCount == 0)
                    _instance = _factory();

                if (_instance is null)
                    throw new InvalidOperationException("The shared disposal manager factory failed.");

                ++_referenceCount;
                return _instance;
            }
        }

        /// <summary>
        /// Releases a reference to the managed object, disposing of the object if it is no longer in use.
        /// </summary>
        /// <param name="obj">The managed object to release.</param>
        /// <returns><c>true</c> if the object was no longer in use and disposed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="obj"/> is not managed by this <see cref="SharedDisposable{T}"/> instance.</exception>
        public bool Release(T obj)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            lock (_lock)
            {
                if (!ReferenceEquals(_instance, obj))
                    throw new ArgumentException("The object is not managed by this shared disposal manager.", nameof(obj));

                if (--_referenceCount == 0)
                {
                    _instance.Dispose();
                    _instance = null;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified <paramref name="obj"/> is the one managed by this <see cref="SharedDisposable{T}"/> instance.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is managed by this <see cref="SharedDisposable{T}"/> instance; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(T? obj) => obj is not null && ReferenceEquals(_instance, obj);
    }
}
