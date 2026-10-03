// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.Retry package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.Retry
{
    using Kampute.Retry.Strategies.Modifiers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="IRetryStrategy"/> to limit and spread its retries, start sessions, and run operations with retries.
    /// </summary>
    public static class RetryStrategyExtensions
    {
        /// <summary>
        /// Enhances a retry strategy with jitter to add randomness to the retry delay.
        /// </summary>
        /// <param name="source">The original retry strategy to be enhanced.</param>
        /// <param name="jitterFactor">The factor by which to adjust the delay randomly, with a default of 0.5.</param>
        /// <returns>A <see cref="JitterStrategyModifier"/> instance wrapping the original retry strategy with added jitter.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="jitterFactor"/> is not between 0 and 1.</exception>
        public static JitterStrategyModifier WithJitter(this IRetryStrategy source, double jitterFactor = 0.5) => new(source, jitterFactor);

        /// <summary>
        /// Enhances a retry strategy with a maximum number of retry attempts.
        /// </summary>
        /// <param name="source">The original retry strategy to be enhanced.</param>
        /// <param name="maxAttempts">The maximum number of attempts allowed before giving up.</param>
        /// <returns>A <see cref="LimitedAttemptsStrategyModifier"/> instance wrapping the original retry strategy with a limit on the number of attempts.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static LimitedAttemptsStrategyModifier WithMaxAttempts(this IRetryStrategy source, uint maxAttempts) => new(source, maxAttempts);

        /// <summary>
        /// Enhances a retry strategy with a timeout, limiting the total duration allowed for retry attempts.
        /// </summary>
        /// <param name="source">The original retry strategy to be enhanced.</param>
        /// <param name="timeout">The maximum duration to attempt retries before giving up.</param>
        /// <returns>A <see cref="LimitedDurationStrategyModifier"/> instance wrapping the original retry strategy with a timeout limit.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static LimitedDurationStrategyModifier WithTimeout(this IRetryStrategy source, TimeSpan timeout) => new(source, timeout);

        /// <summary>
        /// Starts a retry session for one operation that the strategy governs.
        /// </summary>
        /// <param name="source">The retry strategy of the session.</param>
        /// <returns>A new <see cref="RetrySession"/>, with no attempts and its elapsed time starting now.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static RetrySession StartSession(this IRetryStrategy source) => new(source);

        /// <summary>
        /// Runs an asynchronous operation, and retries it as the strategy decides when it fails.
        /// </summary>
        /// <param name="strategy">The retry strategy that decides whether and when to retry.</param>
        /// <param name="operation">The operation to run. It receives <paramref name="cancellationToken"/>.</param>
        /// <param name="retryOn">
        /// A function that returns <see langword="true"/> for the exceptions that should be retried (optional). If <see langword="null"/>, every
        /// exception is retried.
        /// </param>
        /// <param name="cancellationToken">A token for canceling the operation and the waits between attempts (optional).</param>
        /// <returns>A task that completes when the operation succeeds.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategy"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled while waiting before a retry.</exception>
        /// <remarks>
        /// <para>
        /// When the operation throws an exception that <paramref name="retryOn"/> accepts, the method waits as the strategy decides and runs the operation
        /// again. When the strategy allows no more retries, or the exception is not accepted, the last exception is rethrown with its original stack trace.
        /// </para>
        /// <para>
        /// An <see cref="OperationCanceledException"/> thrown after <paramref name="cancellationToken"/> has been canceled is never retried.
        /// </para>
        /// </remarks>
        public static Task ExecuteAsync(this IRetryStrategy strategy, Func<CancellationToken, Task> operation, Func<Exception, bool>? retryOn = null, CancellationToken cancellationToken = default)
        {
            if (strategy is null)
                throw new ArgumentNullException(nameof(strategy));
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            return strategy.ExecuteAsync(async ct =>
            {
                await operation(ct).ConfigureAwait(false);
                return true;
            }, retryOn, cancellationToken);
        }

        /// <summary>
        /// Runs an asynchronous operation that returns a value, and retries it as the strategy decides when it fails.
        /// </summary>
        /// <typeparam name="T">The type of the value the operation returns.</typeparam>
        /// <param name="strategy">The retry strategy that decides whether and when to retry.</param>
        /// <param name="operation">The operation to run. It receives <paramref name="cancellationToken"/>.</param>
        /// <param name="retryOn">
        /// A function that returns <see langword="true"/> for the exceptions that should be retried (optional). If <see langword="null"/>, every
        /// exception is retried.
        /// </param>
        /// <param name="cancellationToken">A token for canceling the operation and the waits between attempts (optional).</param>
        /// <returns>A task that resolves to the value returned by the first successful run of the operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategy"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled while waiting before a retry.</exception>
        /// <remarks>
        /// <para>
        /// When the operation throws an exception that <paramref name="retryOn"/> accepts, the method waits as the strategy decides and runs the operation
        /// again. When the strategy allows no more retries, or the exception is not accepted, the last exception is rethrown with its original stack trace.
        /// </para>
        /// <para>
        /// An <see cref="OperationCanceledException"/> thrown after <paramref name="cancellationToken"/> has been canceled is never retried.
        /// </para>
        /// </remarks>
        public static Task<T> ExecuteAsync<T>(this IRetryStrategy strategy, Func<CancellationToken, Task<T>> operation, Func<Exception, bool>? retryOn = null, CancellationToken cancellationToken = default)
        {
            if (strategy is null)
                throw new ArgumentNullException(nameof(strategy));
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            return ExecuteCoreAsync(strategy.StartSession(), operation, retryOn, cancellationToken);
        }

        /// <summary>
        /// Runs a blocking operation, and retries it as the strategy decides when it fails.
        /// </summary>
        /// <param name="strategy">The retry strategy that decides whether and when to retry.</param>
        /// <param name="operation">The operation to run. It receives <paramref name="cancellationToken"/>.</param>
        /// <param name="retryOn">
        /// A function that returns <see langword="true"/> for the exceptions that should be retried (optional). If <see langword="null"/>, every
        /// exception is retried.
        /// </param>
        /// <param name="cancellationToken">A token for canceling the operation and the waits between attempts (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategy"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled while waiting before a retry.</exception>
        /// <remarks>
        /// <para>
        /// This method blocks the calling thread while it waits between attempts. It behaves as <see cref="ExecuteAsync"/> otherwise: when the
        /// operation throws an exception that <paramref name="retryOn"/> accepts, the method waits as the strategy decides and runs the operation again,
        /// and when the strategy allows no more retries, or the exception is not accepted, the last exception is rethrown with its original stack trace.
        /// </para>
        /// <para>
        /// An <see cref="OperationCanceledException"/> thrown after <paramref name="cancellationToken"/> has been canceled is never retried, and a wait
        /// ends as soon as the token is canceled.
        /// </para>
        /// </remarks>
        public static void Execute(this IRetryStrategy strategy, Action<CancellationToken> operation, Func<Exception, bool>? retryOn = null, CancellationToken cancellationToken = default)
        {
            if (strategy is null)
                throw new ArgumentNullException(nameof(strategy));
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            strategy.Execute(ct =>
            {
                operation(ct);
                return true;
            }, retryOn, cancellationToken);
        }

        /// <summary>
        /// Runs a blocking operation that returns a value, and retries it as the strategy decides when it fails.
        /// </summary>
        /// <typeparam name="T">The type of the value the operation returns.</typeparam>
        /// <param name="strategy">The retry strategy that decides whether and when to retry.</param>
        /// <param name="operation">The operation to run. It receives <paramref name="cancellationToken"/>.</param>
        /// <param name="retryOn">
        /// A function that returns <see langword="true"/> for the exceptions that should be retried (optional). If <see langword="null"/>, every
        /// exception is retried.
        /// </param>
        /// <param name="cancellationToken">A token for canceling the operation and the waits between attempts (optional).</param>
        /// <returns>The value returned by the first successful run of the operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strategy"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled while waiting before a retry.</exception>
        /// <remarks>
        /// <para>
        /// This method blocks the calling thread while it waits between attempts. It behaves as <see cref="ExecuteAsync{T}"/> otherwise: when the
        /// operation throws an exception that <paramref name="retryOn"/> accepts, the method waits as the strategy decides and runs the operation again,
        /// and when the strategy allows no more retries, or the exception is not accepted, the last exception is rethrown with its original stack trace.
        /// </para>
        /// <para>
        /// An <see cref="OperationCanceledException"/> thrown after <paramref name="cancellationToken"/> has been canceled is never retried, and a wait
        /// ends as soon as the token is canceled.
        /// </para>
        /// </remarks>
        public static T Execute<T>(this IRetryStrategy strategy, Func<CancellationToken, T> operation, Func<Exception, bool>? retryOn = null, CancellationToken cancellationToken = default)
        {
            if (strategy is null)
                throw new ArgumentNullException(nameof(strategy));
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            var session = strategy.StartSession();
            for (; ; )
            {
                try
                {
                    return operation(cancellationToken);
                }
                catch (Exception error) when (IsRetryable(error, retryOn, cancellationToken))
                {
                    if (!session.Wait(cancellationToken))
                        throw;
                }
            }
        }

        /// <summary>
        /// Runs the operation of <see cref="ExecuteAsync{T}"/> with retries, after its arguments have been validated.
        /// </summary>
        /// <typeparam name="T">The type of the value the operation returns.</typeparam>
        /// <param name="session">The retry session of the operation.</param>
        /// <param name="operation">The operation to run.</param>
        /// <param name="retryOn">The function that selects the exceptions to retry, or <see langword="null"/> to retry every exception.</param>
        /// <param name="cancellationToken">A token for canceling the operation and the waits between attempts.</param>
        /// <returns>A task that resolves to the value returned by the first successful run of the operation.</returns>
        private static async Task<T> ExecuteCoreAsync<T>(RetrySession session, Func<CancellationToken, Task<T>> operation, Func<Exception, bool>? retryOn, CancellationToken cancellationToken)
        {
            for (; ; )
            {
                try
                {
                    return await operation(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception error) when (IsRetryable(error, retryOn, cancellationToken))
                {
                    if (!await session.WaitAsync(cancellationToken).ConfigureAwait(false))
                        throw;
                }
            }
        }

        /// <summary>
        /// Determines whether a failure of the operation may be retried.
        /// </summary>
        /// <param name="error">The exception the operation threw.</param>
        /// <param name="retryOn">The function that selects the exceptions to retry, or <see langword="null"/> to retry every exception.</param>
        /// <param name="cancellationToken">The token of the caller.</param>
        /// <returns><see langword="true"/> if the failure may be retried; <see langword="false"/> if it reports the caller's cancellation or <paramref name="retryOn"/> rejects it.</returns>
        private static bool IsRetryable(Exception error, Func<Exception, bool>? retryOn, CancellationToken cancellationToken)
        {
            if (error is OperationCanceledException && cancellationToken.IsCancellationRequested)
                return false;

            return retryOn is null || retryOn(error);
        }
    }
}
