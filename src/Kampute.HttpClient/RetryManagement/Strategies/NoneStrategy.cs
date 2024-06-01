// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryManagement.Strategies
{
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// A retry strategy that always indicates no retry should be attempted.
    /// </summary>
    public sealed class NoneStrategy : IRetryStrategy
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="NoneStrategy"/> class from being created.
        /// </summary>
        private NoneStrategy() { }

        /// <summary>
        /// Gets the singleton instance of the <see cref="NoneStrategy"/> class.
        /// </summary>
        /// <value>
        /// The singleton instance of the <see cref="NoneStrategy"/> class.
        /// </value>
        public static NoneStrategy Instance { get; } = new NoneStrategy();

        /// <summary>
        /// Always returns <see langword="false"/> to indicate that no further retry attempts should be made, setting the delay to its default value.
        /// </summary>
        /// <param name="elapsed">The total time elapsed since the start of retry attempts. This parameter is ignored in this implementation.</param>
        /// <param name="attempts">The number of retry attempts made so far. This parameter is ignored in this implementation.</param>
        /// <param name="delay">When this method returns, contains the default value for <see cref="TimeSpan"/>, indicating no delay. This parameter is passed uninitialized.</param>
        /// <returns>Always returns <see langword="false"/>, indicating that no further retry attempts should be made.</returns>
        public bool TryGetRetryDelay(TimeSpan elapsed, uint attempts, out TimeSpan delay)
        {
            delay = default;
            return false;
        }
    }
}
