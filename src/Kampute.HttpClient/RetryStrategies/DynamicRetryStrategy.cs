// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryStrategies
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.HttpClient.RetrySchedulers;
    using System;

    /// <summary>
    /// Represents a backoff strategy that dynamically determines the retry strategy based on runtime conditions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="DynamicRetryStrategy"/> class facilitates the implementation of advanced backoff strategies that can adapt to 
    /// specific conditions encountered during HTTP request execution. By utilizing a factory function to generate <see cref="IRetryScheduler"/>
    /// instances, this class allows for the selection of different backoff mechanisms based on the comprehensive context of the failed 
    /// request, encapsulated within an <see cref="HttpRequestErrorContext"/>. This includes details about the client making the request, 
    /// the request itself, and any exceptions that may have triggered the retry logic.
    /// </para>
    /// <para>
    /// This approach provides a high degree of flexibility, enabling sophisticated retry logic that can optimize performance and reliability 
    /// in complex distributed systems. It is particularly useful in scenarios where different types of errors or operational conditions call 
    /// for varied backoff responses.
    /// </para>
    /// </remarks>
    public class DynamicRetryStrategy : IRetryStrategy
    {
        private readonly Func<HttpRequestErrorContext, IRetryScheduler> _schedulerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicRetryStrategy"/> class with a specified strategy factory.
        /// </summary>
        /// <param name="strategyFactory">A factory function that creates <see cref="IRetryStrategy"/> instances based on the 
        /// context of a failed HTTP request, as encapsulated by an <see cref="HttpRequestErrorContext"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategyFactory"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The <paramref name="strategyFactory"/> delegate allows for the flexible and dynamic selection of backoff strategies based on the 
        /// comprehensive context provided by the <see cref="HttpRequestErrorContext"/>. This design enables the application of tailored retry 
        /// strategies that can adapt to specific error conditions, request details, and client configurations.
        /// </remarks>
        public DynamicRetryStrategy(Func<HttpRequestErrorContext, IRetryStrategy> strategyFactory)
        {
            if (strategyFactory is null)
                throw new ArgumentNullException(nameof(strategyFactory));

            _schedulerFactory = ctx => strategyFactory(ctx).CreateScheduler(ctx);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicRetryStrategy"/> class with a specified scheduler factory.
        /// </summary>
        /// <param name="schedulerFactory">A factory function that creates <see cref="IRetryScheduler"/> instances based on the 
        /// context of a failed HTTP request, as encapsulated by an <see cref="HttpRequestErrorContext"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedulerFactory"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The <paramref name="schedulerFactory"/> delegate allows for the flexible and dynamic selection of backoff strategies based on the 
        /// comprehensive context provided by the <see cref="HttpRequestErrorContext"/>. This design enables the application of tailored retry 
        /// strategies that can adapt to specific error conditions, request details, and client configurations.
        /// </remarks>
        public DynamicRetryStrategy(Func<HttpRequestErrorContext, IRetryScheduler> schedulerFactory)
        {
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        }

        /// <summary>
        /// Creates a scheduler responsible for scheduling HTTP request retry attempts according to the dynamically determined strategy.
        /// </summary>
        /// <param name="ctx">The context containing detailed information about the failed HTTP request, including the client, request, and error information.</param>
        /// <returns>An instance of <see cref="IRetryScheduler"/> that will manage the retry attempts.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <c>null</c>.</exception>
        public IRetryScheduler CreateScheduler(HttpRequestErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            return _schedulerFactory(ctx) ?? ZeroRetryScheduler.Instance;
        }
    }
}
