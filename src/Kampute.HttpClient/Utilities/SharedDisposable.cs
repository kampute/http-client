// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Threading;

    /// <summary>
    /// Manages shared access to a disposable resource, ensuring it is correctly disposed of when no longer in use.
    /// </summary>
    /// <typeparam name="T">The type of the disposable object. Must be a class that implements <see cref="IDisposable"/>.</typeparam>
    /// <remarks>
    /// <para>
    /// This class is particularly useful for managing resources that are expensive to create and can be safely shared across different parts 
    /// of an application. It ensures that the resource remains alive as long as it is needed and is properly cleaned up afterwards. This pattern
    /// helps prevent resource leaks and promotes efficient resource usage.
    /// </para>
    /// <para>
    /// The implementation is thread-safe, making it suitable for use in multi-threaded environments where resources may be accessed concurrently.
    /// The resource is created lazily, only when it is first requested, and is disposed of when the last reference is released.
    /// </para>
    /// </remarks>
    public sealed class SharedDisposable<T> where T : class, IDisposable
    {
        private T? _instance;
        private int _referenceCount;
        private readonly Func<T>? _factory;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDisposable{T}"/> class that uses the default constructor of <typeparamref name="T"/>.
        /// </summary>
        public SharedDisposable()
        {
        }

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
        /// <value>The number of active references.</value>
        public int ReferenceCount => Volatile.Read(ref _referenceCount);

        /// <summary>
        /// Creates a new reference to the shared disposable resource, increasing the reference count.
        /// </summary>
        /// <returns>A new <see cref="Reference"/> instance.</returns>
        public Reference AcquireReference() => new(this);

        /// <summary>
        /// Increases the reference count for the disposable resource. If it is the first reference, creates the resource using the factory method.
        /// </summary>
        /// <returns>The shared disposable resource instance.</returns>
        private T IncReferenceCount()
        {
            lock (_lock)
            {
                if (++_referenceCount == 1)
                    _instance = _factory is not null ? _factory() : (T)Activator.CreateInstance(typeof(T));

                return _instance ?? throw new InvalidOperationException("The shared disposal manager factory failed.");
            }
        }

        /// <summary>
        /// Decreases the reference count for the disposable resource. If no more references exist, disposes of the resource.
        /// </summary>
        private void DecReferenceCount()
        {
            lock (_lock)
            {
                if (_instance is not null && --_referenceCount == 0)
                {
                    _instance.Dispose();
                    _instance = null;
                }
            }
        }

        /// <summary>
        /// Represents a reference to the shared disposable resource.
        /// </summary>
        public sealed class Reference : IDisposable
        {
            private readonly SharedDisposable<T> _owner;
            private T? _instance;

            /// <summary>
            /// Initializes a new instance of the <see cref="Reference"/> class, increasing the reference count of the resource.
            /// </summary>
            /// <param name="owner">The <see cref="SharedDisposable{T}"/> instance that owns this reference.</param>
            internal Reference(SharedDisposable<T> owner)
            {
                _owner = owner;
                _instance = _owner.IncReferenceCount();
            }

            /// <summary>
            /// Gets the <see cref="SharedDisposable{T}"/> instance that owns this reference.
            /// </summary>
            public SharedDisposable<T> Owner => _owner;

            /// <summary>
            /// Gets the instance of the shared disposable resource.
            /// </summary>
            /// <value>The shared disposable resource instance.</value>
            /// <exception cref="ObjectDisposedException">Thrown if the reference has been disposed.</exception>
            public T Instance => _instance ?? throw new ObjectDisposedException(typeof(Reference).Name);

            /// <summary>
            /// Decreases the reference count and disposes the resource if it is no longer needed.
            /// </summary>
            public void Dispose()
            {
                if (_instance is not null)
                {
                    _instance = null;
                    _owner.DecReferenceCount();
                }
            }

            /// <summary>
            /// Allows implicit conversion of the <see cref="Reference"/> to the shared resource type.
            /// </summary>
            /// <param name="reference">The reference instance.</param>
            public static implicit operator T(Reference reference) => reference.Instance;
        }
    }
}
