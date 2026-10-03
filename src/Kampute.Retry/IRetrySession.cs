// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.Retry package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.Retry
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the retry state of one operation: it decides whether the operation is retried after a failure, and waits before the retry.
    /// </summary>
    /// <remarks>
    /// A session is created when an operation first fails and is used for all its later failures, so that the attempts it counts and the time it
    /// measures cover the whole operation. <see cref="RetrySession"/> applies an <see cref="IRetryStrategy"/>; other implementations can take the
    /// decision from elsewhere, such as a time suggested by a server.
    /// </remarks>
    public interface IRetrySession
    {
        /// <summary>
        /// Waits for the appropriate time before the next retry attempt and determines whether a retry should be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait.</param>
        /// <returns>A task that resolves to <see langword="true"/> if a retry should be attempted after the wait; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled while waiting.</exception>
        /// <remarks>
        /// Implementations can base the decision on more than elapsed time and attempts, such as guidance from an external service or the current
        /// system load, and must observe <paramref name="cancellationToken"/>.
        /// </remarks>
        Task<bool> WaitAsync(CancellationToken cancellationToken);
    }
}
