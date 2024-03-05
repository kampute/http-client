// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetrySchedulers
{
    using Kampute.HttpClient.RetrySchedulers.Abstracts;
    using System;

    /// <summary>
    /// A scheduler that schedules retries with a uniform delay.
    /// </summary>
    /// <remarks>
    /// The <see cref="UniformRetryScheduler"/> class provides a retry mechanism with a constant wait time between retries. This strategy is 
    /// useful for scenarios where a predictable retry pattern is desired.
    /// </remarks>
    public sealed class UniformRetryScheduler : RetryScheduler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UniformRetryScheduler"/> class.
        /// </summary>
        /// <param name="delay">The delay duration between retry attempts.</param>
        public UniformRetryScheduler(TimeSpan delay)
        {
            Delay = delay;
        }

        /// <summary>
        /// Gets the delay duration between retries.
        /// </summary>
        public override TimeSpan Delay { get; }
    }
}
