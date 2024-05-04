// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.RetryManagement
{
    using Kampute.HttpClient.Interfaces;
    using System;

    /// <summary>
    /// Represents a factory that dynamically creates retry schedulers based on runtime conditions of HTTP requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="DynamicBackoffStrategy"/> class leverages a factory function to instantiate <see cref="IRetryScheduler"/> objects, enabling
    /// the selection of specific retry strategies tailored to the conditions observed during the execution of HTTP requests. The decision-making
    /// process utilizes detailed context provided by <see cref="HttpRequestErrorContext"/>, which includes information about the HTTP client,
    /// the request, and any encountered exceptions.
    /// </para>
    /// <para>
    /// This dynamic approach allows for the implementation of sophisticated retry strategies that can adjust to varying error types and operational
    /// scenarios. It is especially beneficial in complex distributed systems where distinct error conditions or system states may necessitate
    /// different retry behaviors for optimizing overall system performance and reliability.
    /// </para>
    /// </remarks>
    public class DynamicBackoffStrategy : IHttpBackoffProvider
    {
        private readonly Func<HttpRequestErrorContext, IRetryScheduler> _schedulerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicBackoffStrategy"/> with a scheduler factory function.
        /// </summary>
        /// <param name="schedulerFactory">A factory function that produces <see cref="IRetryScheduler"/> instances, allowing for dynamic
        /// selection of retry strategies based on the detailed context of failed HTTP requests.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedulerFactory"/> is <c>null</c>.</exception>
        public DynamicBackoffStrategy(Func<HttpRequestErrorContext, IRetryScheduler> schedulerFactory)
        {
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicBackoffStrategy"/> with a strategy factory function.
        /// </summary>
        /// <param name="strategyFactory">A factory function that produces <see cref="IRetryStrategy"/> instances, allowing for dynamic
        /// selection of retry strategies based on the detailed context of failed HTTP requests.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategyFactory"/> is <c>null</c>.</exception>
        public DynamicBackoffStrategy(Func<HttpRequestErrorContext, IRetryStrategy> strategyFactory)
        {
            if (strategyFactory is null)
                throw new ArgumentNullException(nameof(strategyFactory));

            _schedulerFactory = ctx =>
            {
                var strategy = strategyFactory(ctx);
                return strategy is not null
                    ? strategy.ToScheduler()
                    : throw new InvalidOperationException("The strategy factory function returned null.");
            };
        }

        /// <summary>
        /// Creates a retry scheduler tailored to the specific conditions of a failed HTTP request, as determined by the provided context.
        /// </summary>
        /// <param name="ctx">The context containing detailed information about the failed HTTP request, such as the client, request, and error details.</param>
        /// <returns>An instance of <see cref="IRetryScheduler"/> configured to manage retry attempts for the given request context.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the scheduler factory function returns <c>null</c>.</exception>
        public IRetryScheduler CreateScheduler(HttpRequestErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            return _schedulerFactory(ctx) ?? throw new InvalidOperationException("The scheduler factory function returned null.");
        }
    }
}
