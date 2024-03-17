namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a thread-safe mechanism for asynchronously updating a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to be updated. It is recommended that this type be immutable to ensure thread-safe operations.</typeparam>
    /// <remarks>
    /// <para>
    /// This class ensures that updates to the value are performed in a thread-safe manner and allows for updates to be deferred if
    /// a more recent update request has been made. It is particularly useful in scenarios where values are being updated from multiple
    /// sources or threads and you want to ensure consistency and prevent race conditions.
    /// </para>
    /// <para>
    /// The value is updated using an asynchronous updater function provided by the caller, which can incorporate custom logic for
    /// updating the value based on its current state and external criteria.
    /// </para>
    /// </remarks>
    public sealed class AsyncGuard<T> : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private VolatileWrapper _lastValue;
        private long _lastUpdateTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncGuard{T}"/> class with an initial value.
        /// </summary>
        /// <param name="initialValue">The initial value of the type <typeparamref name="T"/>.</param>
        public AsyncGuard(T? initialValue)
        {
            _lastValue = new VolatileWrapper(initialValue);
            _lastUpdateTime = 0;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <value>
        /// The current value of type <typeparamref name="T"/>. This value is thread-safe to access and represents the most recent state
        /// managed by the <see cref="AsyncGuard{T}"/>.
        /// </value>
        public T? Value
        {
            get => Volatile.Read(ref _lastValue).Value;
            private set => Volatile.Write(ref _lastValue, new VolatileWrapper(value));
        }

        /// <summary>
        /// Gets the Unix time stamp of the last successful update operation.
        /// </summary>
        /// <value>
        /// The Unix time stamp representing the point in time when the last successful update to the value was made, measured in milliseconds.
        /// If no update has been applied, the value is zero.
        /// </value>
        public long LastUpdateUnixTime
        {
            get => Volatile.Read(ref _lastUpdateTime);
            private set => Volatile.Write(ref _lastUpdateTime, value);
        }

        /// <summary>
        /// Attempts to update the value asynchronously using the provided updater function.
        /// </summary>
        /// <param name="asyncUpdater">The asynchronous function used to update the value.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value that indicates whether the value was updated.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncUpdater"/> is <c>null</c>.</exception>
        /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// <para>
        /// This method allows for a thread-safe update of the value. The update will only be applied if no other update has been completed since
        /// this update attempt was initiated, preventing unnecessary updates or overwrites by concurrent operations.
        /// </para>
        /// <para>
        /// If the update proceeds and is successful, the method returns <c>true</c>; if another update has already been applied, it returns <c>false</c>.
        /// This behavior ensures that the value reflects the most recent update attempt that was actually needed.
        /// </para>
        /// </remarks>
        public async Task<bool> TryUpdateAsync(Func<Task<T?>> asyncUpdater, CancellationToken cancellationToken = default)
        {
            if (asyncUpdater is null)
                throw new ArgumentNullException(nameof(asyncUpdater));

            var acquireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (LastUpdateUnixTime >= acquireTime)
                    return false; // Already updated by another thread.

                Value = await asyncUpdater().ConfigureAwait(false);
                LastUpdateUnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="AsyncGuard{T}"/>.
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
            public VolatileWrapper(T? value)
            {
                Value = value;
            }

            /// <summary>
            /// Gets the value stored in the wrapper.
            /// </summary>
            public readonly T? Value;
        }

        #endregion
    }
}
