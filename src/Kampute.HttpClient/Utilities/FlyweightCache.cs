namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Provides a thread-safe cache for efficiently retrieving and lazily adding key-value pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
    /// <remarks>
    /// This cache utilizes <see cref="ConcurrentDictionary{TKey, TValue}"/> to ensure thread-safe access while optimizing for
    /// high read scenarios and infrequent writes. Values are created on demand using a specified factory method when they
    /// are accessed and not already present, allowing for efficient memory usage and avoiding pre-population overhead.
    /// </remarks>
    public sealed class FlyweightCache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _store;
        private readonly Func<TKey, TValue> _valueFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlyweightCache{TKey, TValue}"/> class using a specified value factory.
        /// </summary>
        /// <param name="valueFactory">A delegate that defines the method to create values if the key does not exist in the cache.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="valueFactory"/> is <c>null</c>.</exception>
        public FlyweightCache(Func<TKey, TValue> valueFactory)
            : this(valueFactory, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlyweightCache{TKey, TValue}"/> class using a specified value factory and key comparer.
        /// </summary>
        /// <param name="valueFactory">A delegate that defines the method to create values if the key does not exist in the cache.</param>
        /// <param name="keyComparer">The equality comparison implementation to use when comparing keys.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="valueFactory"/> is <c>null</c>.</exception>
        public FlyweightCache(Func<TKey, TValue> valueFactory, IEqualityComparer<TKey>? keyComparer)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            _store = new ConcurrentDictionary<TKey, TValue>(keyComparer);
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the cache.
        /// </summary>
        /// <value>The number of key/value pairs currently stored in the cache.</value>
        public int Count => _store.Count;

        /// <summary>
        /// Retrieves a value for the specified key.
        /// </summary>
        /// <param name="key">The key whose value to retrieve.</param>
        /// <value>The value associated with the specified key.</value>
        public TValue this[TKey key] => _store.GetOrAdd(key, _valueFactory);

        /// <summary>
        /// Checks if the cache contains a value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to check in the cache.</param>
        /// <returns><c>true</c> if the key exists in the cache; otherwise, <c>false</c>.</returns>
        public bool Contains(TKey key) => _store.ContainsKey(key);

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear() => _store.Clear();
    }
}
