// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using Kampute.HttpClient.Interfaces;
    using Kampute.Resilience;
    using System;

    /// <summary>
    /// Retries failed HTTP requests as a retry strategy decides.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Create a policy from any <see cref="IRetryStrategy"/> with <see cref="HttpRetryPolicyExtensions.ToHttpRetryPolicy"/>. Each call that fails gets
    /// its own <see cref="RetrySession"/>, so the strategy, and the policy, can be shared by any number of requests and clients.
    /// </para>
    /// <para>
    /// To choose the strategy or the session from the failure, use <see cref="Dynamic(Func{HttpRequestErrorContext, IRetryStrategy})"/> or
    /// <see cref="Dynamic(Func{HttpRequestErrorContext, IRetrySession})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// This policy retries a request up to five times after connection failures, with delays that grow with the Fibonacci sequence from one second:
    /// <code>
    /// client.RetryPolicy = RetryStrategies.Fibonacci(TimeSpan.FromSeconds(1)).WithMaxRetries(5).ToHttpRetryPolicy();
    /// </code>
    /// </example>
    /// <seealso cref="HttpRestClient.RetryPolicy"/>
    public class HttpRetryPolicy : IHttpRetryPolicy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRetryPolicy"/> class with the specified retry strategy.
        /// </summary>
        /// <param name="strategy">The retry strategy that decides whether and when a failed request is retried.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategy"/> is <see langword="null"/>.</exception>
        public HttpRetryPolicy(IRetryStrategy strategy)
        {
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>
        /// Gets a policy that never retries.
        /// </summary>
        /// <value>
        /// A policy based on <see cref="RetryStrategies.None"/>.
        /// </value>
        public static HttpRetryPolicy None { get; } = new(RetryStrategies.None);

        /// <summary>
        /// Gets the retry strategy of this policy.
        /// </summary>
        /// <value>
        /// The <see cref="IRetryStrategy"/> that decides whether and when a failed request is retried.
        /// </value>
        public virtual IRetryStrategy Strategy { get; }

        /// <summary>
        /// Creates a retry session that applies the strategy of this policy to a failed request.
        /// </summary>
        /// <param name="ctx">The context of the failure.</param>
        /// <returns>A new <see cref="RetrySession"/> for <see cref="Strategy"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ctx"/> is <see langword="null"/>.</exception>
        public virtual IRetrySession CreateSession(HttpRequestErrorContext ctx)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            return Strategy.StartSession();
        }

        /// <summary>
        /// Creates a policy that chooses the retry strategy for each failed request.
        /// </summary>
        /// <param name="strategyFactory">A function that returns the retry strategy for the context of a failure.</param>
        /// <returns>A policy that starts a session with the strategy that <paramref name="strategyFactory"/> returns.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategyFactory"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If <paramref name="strategyFactory"/> returns <see langword="null"/>, creating the session throws <see cref="InvalidOperationException"/>.
        /// </remarks>
        public static IHttpRetryPolicy Dynamic(Func<HttpRequestErrorContext, IRetryStrategy> strategyFactory)
        {
            if (strategyFactory is null)
                throw new ArgumentNullException(nameof(strategyFactory));

            return new DynamicPolicy(ctx =>
            {
                var strategy = strategyFactory(ctx) ?? throw new InvalidOperationException("The strategy factory function returned null.");
                return strategy.StartSession();
            });
        }

        /// <summary>
        /// Creates a policy that creates the retry session for each failed request.
        /// </summary>
        /// <param name="sessionFactory">A function that returns the retry session for the context of a failure.</param>
        /// <returns>A policy that uses the session that <paramref name="sessionFactory"/> returns.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionFactory"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If <paramref name="sessionFactory"/> returns <see langword="null"/>, creating the session throws <see cref="InvalidOperationException"/>.
        /// </remarks>
        public static IHttpRetryPolicy Dynamic(Func<HttpRequestErrorContext, IRetrySession> sessionFactory)
        {
            if (sessionFactory is null)
                throw new ArgumentNullException(nameof(sessionFactory));

            return new DynamicPolicy(ctx => sessionFactory(ctx) ?? throw new InvalidOperationException("The session factory function returned null."));
        }

        /// <summary>
        /// A policy that delegates the creation of each session to a function.
        /// </summary>
        private sealed class DynamicPolicy : IHttpRetryPolicy
        {
            private readonly Func<HttpRequestErrorContext, IRetrySession> _sessionFactory;

            public DynamicPolicy(Func<HttpRequestErrorContext, IRetrySession> sessionFactory) => _sessionFactory = sessionFactory;

            public IRetrySession CreateSession(HttpRequestErrorContext ctx)
            {
                if (ctx is null)
                    throw new ArgumentNullException(nameof(ctx));

                return _sessionFactory(ctx);
            }
        }
    }
}
