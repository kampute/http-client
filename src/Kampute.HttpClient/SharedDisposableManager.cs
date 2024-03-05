// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Manages shared access to a disposable resource, ensuring it is correctly disposed of when no longer in use.
    /// </summary>
    /// <remarks>
    /// This class is particularly useful for managing resources that are expensive to create and can be safely shared across different parts 
    /// of an application. It ensures that the resource remains alive as long as it is needed and is properly cleaned up afterwards. This pattern
    /// helps prevent resource leaks and promotes efficient resource usage.
    /// </remarks>
    /// <typeparam name="T">The type of the disposable object. Must be a class that implements <see cref="IDisposable"/>.</typeparam>
    public class SharedDisposableManager<T> where T : class, IDisposable
    {
        private T? _instance;
        private int _referenceCount = 0;
        private readonly Func<T> _factory;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDisposableManager{T}"/> class with a factory function.
        /// </summary>
        /// <param name="factory">A function that creates an instance of the object <typeparamref name="T"/> when needed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <c>null</c>.</exception>
        public SharedDisposableManager(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Acquires an instance of the managed object, incrementing the reference count and creating the object if necessary.
        /// </summary>
        /// <returns>The managed instance of the object.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the factory fails to create an instance.</exception>
        public T Acquire()
        {
            lock (_lock)
            {
                if (_referenceCount++ == 0)
                    _instance = _factory();

                return _instance ?? throw new InvalidOperationException("The factory failed to create an instance. Ensure the factory is properly configured.");
            }
        }

        /// <summary>
        /// Releases a reference to the managed object, decrementing the reference count and disposing of the object if it reaches zero.
        /// </summary>
        /// <param name="instance">The instance to release.</param>
        /// <returns><c>true</c> if the object was disposed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="instance"/> is not managed by this reference counter.</exception>
        public bool Release(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            lock (_lock)
            {
                if (!Is(instance))
                    throw new ArgumentException("The instance is not managed by this reference counter.", nameof(instance));

                if (--_referenceCount == 0)
                {
                    _instance?.Dispose();
                    _instance = null;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified <paramref name="instance"/> is the one managed by this reference counter.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns><c>true</c> if <paramref name="instance"/> is the managed instance; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(T instance) => ReferenceEquals(_instance, instance);
    }
}
