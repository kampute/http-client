namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages thread-safe, asynchronous updates to a value, ensuring efficiency by reducing unnecessary update operations.
    /// </summary>
    /// <typeparam name="T">The type of the value to be managed, preferably immutable for thread safety.</typeparam>
    /// <remarks>
    /// This class provides a mechanism to update a value asynchronously while ensuring that updates are serialized and efficient. It is designed to prevent multiple,
    /// concurrent update operations from being processed if they are requested in quick succession. By employing a timing check before applying updates, the class ensures
    /// that only necessary updates proceed when the value has not been recently updated, making it ideal for scenarios where collecting or calculating the updated value is
    /// resource-intensive or costly.
    /// </remarks>
    public sealed class AsyncUpdateThrottle<T> : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private VolatileWrapper _value;
        private long _lastUpdateTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncUpdateThrottle{T}"/> class with a default value.
        /// </summary>
        public AsyncUpdateThrottle()
            : this(default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncUpdateThrottle{T}"/> class with a specified value.
        /// </summary>
        /// <param name="initialValue">The initial value of the type <typeparamref name="T"/>.</param>
        public AsyncUpdateThrottle(T? initialValue)
        {
            _value = new VolatileWrapper(initialValue);
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <value>
        /// The current value of type <typeparamref name="T"/>. This value is thread-safe to access and represents the most recent state
        /// managed by the <see cref="AsyncUpdateThrottle{T}"/>.
        /// </value>
        public T? Value
        {
            get => Volatile.Read(ref _value).Value;
            private set => Volatile.Write(ref _value, new VolatileWrapper(value));
        }

        /// <summary>
        /// Gets the time of the last successful update operation.
        /// </summary>
        /// <value>
        /// The time when the last successful update to the value was made. If no update has been applied, the value is <see cref="DateTimeOffset.MinValue"/>.
        /// </value>
        public DateTimeOffset LastUpdateTime
        {
            get => DateTimeOffset.FromUnixTimeMilliseconds(Volatile.Read(ref _lastUpdateTime));
            private set => Volatile.Write(ref _lastUpdateTime, value.ToUnixTimeMilliseconds());
        }

        /// <summary>
        /// Attempts to update the value asynchronously using the provided updater function.
        /// </summary>
        /// <param name="asyncUpdater">The asynchronous function used to update the value.</param>
        /// <param name="cancellationToken">A token for canceling the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value that indicates whether the value was updated.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncUpdater"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
        /// <remarks>
        /// <para>
        /// This method allows for a thread-safe update of the value. The update will only be applied if no other update has been completed since
        /// this update attempt was initiated, preventing unnecessary updates or overwrites by concurrent operations.
        /// </para>
        /// <para>
        /// If the update proceeds and is successful, the method returns <see langword="true"/>; if another update has already been applied, it returns <see langword="false"/>.
        /// This behavior ensures that the value reflects the most recent update attempt that was actually needed.
        /// </para>
        /// </remarks>
        public async Task<bool> TryUpdateAsync(Func<Task<T?>> asyncUpdater, CancellationToken cancellationToken = default)
        {
            if (asyncUpdater is null)
                throw new ArgumentNullException(nameof(asyncUpdater));

            var requestTime = DateTimeOffset.UtcNow;
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (requestTime <= LastUpdateTime)
                    return false; // The value is already up to date.

                Value = await asyncUpdater().ConfigureAwait(false);
                LastUpdateTime = DateTimeOffset.UtcNow;
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="AsyncUpdateThrottle{T}"/>.
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
