// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Threading;

    /// <summary>
    /// Shares one instance of a disposable resource among its users, and disposes it when the last of them releases it.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <remarks>
    /// Each user acquires a <see cref="Reference"/> and disposes it when done. The resource is created when the first reference is acquired, and
    /// disposed when the last reference is disposed; a later reference creates a new instance. The class is thread-safe.
    /// </remarks>
    public sealed class SharedDisposable<T> where T : class, IDisposable
    {
        private T? _instance;
        private int _referenceCount;
        private readonly Func<T> _factory;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDisposable{T}"/> class that creates the resource with the parameterless constructor of <typeparamref name="T"/>.
        /// </summary>
        public SharedDisposable()
            : this(Activator.CreateInstance<T>)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDisposable{T}"/> class that creates the resource with a function.
        /// </summary>
        /// <param name="factory">The function that creates the resource.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <see langword="null"/>.</exception>
        public SharedDisposable(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Gets the number of references that are not yet disposed.
        /// </summary>
        /// <value>The number of active references.</value>
        public int ReferenceCount => Volatile.Read(ref _referenceCount);

        /// <summary>
        /// Acquires a reference to the resource, creating the resource if no other reference is active.
        /// </summary>
        /// <returns>A new <see cref="Reference"/>, which releases the resource when it is disposed.</returns>
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
                    _instance = _factory();

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
        /// Represents one user's reference to the shared resource.
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
            /// Gets the <see cref="SharedDisposable{T}"/> that the reference belongs to.
            /// </summary>
            /// <value>The <see cref="SharedDisposable{T}"/> of this reference.</value>
            public SharedDisposable<T> Owner => _owner;

            /// <summary>
            /// Gets the shared resource.
            /// </summary>
            /// <value>The shared instance of <typeparamref name="T"/>.</value>
            /// <exception cref="ObjectDisposedException">Thrown if the reference has been disposed.</exception>
            public T Instance => _instance ?? throw new ObjectDisposedException(typeof(Reference).Name);

            /// <summary>
            /// Releases the reference, and disposes the resource if this was the last active reference.
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
            /// Returns the shared resource of a reference.
            /// </summary>
            /// <param name="reference">The reference.</param>
            /// <returns>The <see cref="Instance"/> of <paramref name="reference"/>.</returns>
            public static implicit operator T(Reference reference) => reference.Instance;
        }
    }
}
