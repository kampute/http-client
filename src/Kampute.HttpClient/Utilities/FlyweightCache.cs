namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Provides a thread-safe cache that creates the value of a key on first use and returns the same value afterwards.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
    /// <remarks>
    /// Entries are never removed except by <see cref="Clear"/>, so use the cache for a small, bounded set of keys. When two threads ask for a
    /// missing key at the same time, the factory can run twice, but both get the same value.
    /// </remarks>
    public sealed class FlyweightCache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _store;
        private readonly Func<TKey, TValue> _valueFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlyweightCache{TKey, TValue}"/> class using a specified value factory.
        /// </summary>
        /// <param name="valueFactory">The function that creates the value of a key.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="valueFactory"/> is <see langword="null"/>.</exception>
        public FlyweightCache(Func<TKey, TValue> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            _store = new ConcurrentDictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlyweightCache{TKey, TValue}"/> class using a specified value factory and key comparer.
        /// </summary>
        /// <param name="valueFactory">The function that creates the value of a key.</param>
        /// <param name="keyComparer">The comparer that decides whether two keys are equal.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="valueFactory"/> or <paramref name="keyComparer"/> is <see langword="null"/>.</exception>
        public FlyweightCache(Func<TKey, TValue> valueFactory, IEqualityComparer<TKey> keyComparer)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            _store = new ConcurrentDictionary<TKey, TValue>(keyComparer ?? throw new ArgumentNullException(nameof(keyComparer)));
        }

        /// <summary>
        /// Gets the number of cached values.
        /// </summary>
        /// <value>The number of keys in the cache.</value>
        public int Count => _store.Count;

        /// <summary>
        /// Returns the value of a key, creating it if the cache has none.
        /// </summary>
        /// <param name="key">The key whose value to return.</param>
        /// <returns>The cached value of <paramref name="key"/>.</returns>
        public TValue Get(TKey key) => _store.GetOrAdd(key, _valueFactory);

        /// <summary>
        /// Determines whether the cache has a value for a key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns><see langword="true"/> if the key exists in the cache; otherwise, <see langword="false"/>.</returns>
        public bool Contains(TKey key) => _store.ContainsKey(key);

        /// <summary>
        /// Removes all values from the cache.
        /// </summary>
        public void Clear() => _store.Clear();
    }
}
