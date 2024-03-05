// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetrySchedulers
{
    using Kampute.HttpClient.Interfaces;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A scheduler that always indicates no retry should be attempted.
    /// </summary>
    public sealed class ZeroRetryScheduler : IRetryScheduler
    {
        private ZeroRetryScheduler() { }

        /// <summary>
        /// Gets the singleton instance of the <see cref="ZeroRetryScheduler"/> class.
        /// </summary>
        public static ZeroRetryScheduler Instance { get; } = new();

        /// <summary>
        /// Determines whether a retry attempt should be made, which is always <c>false</c> for this scheduler.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        /// <returns>A task that resolves to <c>false</c>, indicating that no retry should be attempted.</returns>
        public Task<bool> WaitAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
