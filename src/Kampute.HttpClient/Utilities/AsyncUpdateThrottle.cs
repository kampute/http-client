namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Holds a value that is updated asynchronously, and skips an update when another one completed while it waited.
    /// </summary>
    /// <typeparam name="T">The type of the value. An immutable type is safest, because the value is shared between threads.</typeparam>
    /// <remarks>
    /// Updates run one at a time. When several callers request an update at the same time, such as requests that all find an access token expired,
    /// the first one runs and the others return without running their update, because the value changed after they asked. This suits values that
    /// are costly to obtain.
    /// </remarks>
    public sealed class AsyncUpdateThrottle<T> : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private VolatileWrapper _value;
        private long _lastUpdateTime;
        private int _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncUpdateThrottle{T}"/> class with the default value of <typeparamref name="T"/>.
        /// </summary>
        public AsyncUpdateThrottle()
            : this(default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncUpdateThrottle{T}"/> class with the specified value.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        public AsyncUpdateThrottle(T? initialValue)
        {
            _value = new VolatileWrapper(initialValue);
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <value>
        /// The value of the last completed update, or the initial value if none has completed. It can be read from any thread.
        /// </value>
        public T? Value
        {
            get => Volatile.Read(ref _value).Value;
            private set => Volatile.Write(ref _value, new VolatileWrapper(value));
        }

        /// <summary>
        /// Gets the time of the last completed update.
        /// </summary>
        /// <value>
        /// The time, in UTC, when the last update completed, or <see cref="DateTimeOffset.MinValue"/> if none has completed.
        /// </value>
        public DateTimeOffset LastUpdateTime
        {
            get => new(Volatile.Read(ref _lastUpdateTime), TimeSpan.Zero);
            private set => Volatile.Write(ref _lastUpdateTime, value.UtcTicks);
        }

        /// <summary>
        /// Updates the value with the result of a function, unless another update completes after this method is called.
        /// </summary>
        /// <param name="asyncUpdater">The function that produces the new value.</param>
        /// <param name="cancellationToken">A token for canceling the wait for an update in progress.</param>
        /// <returns>
        /// A task that resolves to <see langword="true"/> if <paramref name="asyncUpdater"/> ran and its result became the value, or <see langword="false"/>
        /// if another update completed first and <paramref name="asyncUpdater"/> did not run.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncUpdater"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled while the method waits for an update in progress.</exception>
        /// <remarks>
        /// If <paramref name="asyncUpdater"/> throws, the value is unchanged and the exception propagates to the caller.
        /// </remarks>
        public Task<bool> TryUpdateAsync(Func<Task<T?>> asyncUpdater, CancellationToken cancellationToken = default)
        {
            if (asyncUpdater is null)
                throw new ArgumentNullException(nameof(asyncUpdater));

            return TryUpdateCoreAsync(asyncUpdater, Volatile.Read(ref _version), cancellationToken);
        }

        /// <summary>
        /// Updates the value unless another update has completed since <paramref name="requestVersion"/> was read.
        /// </summary>
        /// <param name="asyncUpdater">The function that produces the new value.</param>
        /// <param name="requestVersion">The version of the value when the update was requested.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that resolves to <see langword="true"/> if the value was updated; otherwise, <see langword="false"/>.</returns>
        private async Task<bool> TryUpdateCoreAsync(Func<Task<T?>> asyncUpdater, int requestVersion, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (requestVersion != _version)
                    return false; // The value is already up to date.

                Value = await asyncUpdater().ConfigureAwait(false);
                LastUpdateTime = DateTimeOffset.UtcNow;
                Interlocked.Increment(ref _version);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Releases the lock that serializes the updates.
        /// </summary>
        public void Dispose()
        {
            _semaphore.Dispose();
        }

        #region Helper Type


        /// <summary>
        /// A wrapper class to hold the value. This ensures that reads and writes to the value are volatile and the
        /// most up-to-date value is always read.
        /// </summary>
        private class VolatileWrapper
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="VolatileWrapper"/> class.
            /// </summary>
            /// <param name="value">The initial value.</param>
            public VolatileWrapper(T? value) => Value = value;

            /// <summary>
            /// Gets the value stored in the wrapper.
            /// </summary>
            public readonly T? Value;
        }

        #endregion
    }
}
