// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for scheduling retry attempts for an operation. This includes determining if a retry is appropriate 
    /// and managing the waiting period before the next attempt.
    /// </summary>
    public interface IRetryScheduler
    {
        /// <summary>
        /// Waits for the appropriate time before the next retry attempt, according to the scheduler's strategy, and determines if a retry should 
        /// be attempted.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        /// <returns>A task that resolves to <c>true</c> if a retry should be attempted; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// The asynchronous nature of the method allows implementations to incorporate not just simple time-based waiting but also 
        /// more complex logic, such as querying external services for guidance on when to retry or dynamically adjusting backoff 
        /// parameters in response to system load. 
        /// </para>
        /// <para>
        /// Implementations should respect the provided <see cref="CancellationToken"/> to ensure that the application remains responsive, 
        /// especially during shutdown sequences or when operations need to be canceled prematurely.
        /// </para>
        /// </remarks>
        Task<bool> WaitAsync(CancellationToken cancellationToken);
    }
}
