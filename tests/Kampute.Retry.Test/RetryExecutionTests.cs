namespace Kampute.Retry.Test
{
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class RetryExecutionTests
    {
        private static readonly IRetryStrategy ImmediateRetries = RetryStrategies.Uniform(TimeSpan.Zero).WithMaxAttempts(3);

        [Test]
        public async Task ExecuteAsync_WhenOperationSucceeds_RunsItOnce()
        {
            var runs = 0;

            await ImmediateRetries.ExecuteAsync(_ => { ++runs; return Task.CompletedTask; });

            Assert.That(runs, Is.EqualTo(1));
        }

        [Test]
        public async Task ExecuteAsync_WhenOperationFailsThenSucceeds_RetriesUntilSuccess()
        {
            var runs = 0;

            var result = await ImmediateRetries.ExecuteAsync(_ => ++runs < 3 ? throw new IOException() : Task.FromResult(runs));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runs, Is.EqualTo(3));
                Assert.That(result, Is.EqualTo(3));
            }
        }

        [Test]
        public void ExecuteAsync_WhenStrategyStops_RethrowsTheLastExceptionWithItsStackTrace()
        {
            var runs = 0;

            var exception = Assert.ThrowsAsync<IOException>(() => ImmediateRetries.ExecuteAsync(_ => FailAsync(++runs)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runs, Is.EqualTo(4));
                Assert.That(exception.Message, Is.EqualTo("Failure 4"));
                Assert.That(exception.StackTrace, Does.Contain(nameof(FailAsync)));
            }
        }

        [Test]
        public void ExecuteAsync_WhenRetryOnRejectsTheException_DoesNotRetry()
        {
            var runs = 0;

            Assert.ThrowsAsync<InvalidOperationException>(() => ImmediateRetries.ExecuteAsync(_ =>
            {
                ++runs;
                throw new InvalidOperationException();
            }, retryOn: error => error is IOException));

            Assert.That(runs, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteAsync_WhenCanceledWhileWaiting_ThrowsOperationCanceledException()
        {
            var retry = RetryStrategies.Uniform(TimeSpan.FromMinutes(1));
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            var timer = Stopwatch.StartNew();
            Assert.CatchAsync<OperationCanceledException>(() => retry.ExecuteAsync(_ => throw new IOException(), cancellationToken: cancellationTokenSource.Token));
            timer.Stop();

            Assert.That(timer.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void ExecuteAsync_WhenOperationReportsCallerCancellation_DoesNotRetry()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var runs = 0;

            Assert.CatchAsync<OperationCanceledException>(() => ImmediateRetries.ExecuteAsync(ct =>
            {
                ++runs;
                cancellationTokenSource.Cancel();
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }, cancellationToken: cancellationTokenSource.Token));

            Assert.That(runs, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteAsync_WithNullArgument_ThrowsBeforeReturningTask()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentNullException>(() => ((IRetryStrategy)null!).ExecuteAsync(_ => Task.CompletedTask));
                Assert.Throws<ArgumentNullException>(() => ImmediateRetries.ExecuteAsync((Func<CancellationToken, Task>)null!));
                Assert.Throws<ArgumentNullException>(() => ImmediateRetries.ExecuteAsync((Func<CancellationToken, Task<int>>)null!));
            }
        }

        [Test]
        public void Execute_WhenOperationSucceeds_RunsItOnce()
        {
            var runs = 0;

            ImmediateRetries.Execute(_ => ++runs);

            Assert.That(runs, Is.EqualTo(1));
        }

        [Test]
        public void Execute_WhenOperationFailsThenSucceeds_RetriesUntilSuccess()
        {
            var runs = 0;

            var result = ImmediateRetries.Execute(_ => ++runs < 3 ? throw new IOException() : runs);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runs, Is.EqualTo(3));
                Assert.That(result, Is.EqualTo(3));
            }
        }

        [Test]
        public void Execute_WhenStrategyStops_RethrowsTheLastExceptionWithItsStackTrace()
        {
            var runs = 0;

            var exception = Assert.Throws<IOException>(() => ImmediateRetries.Execute(_ => Fail(++runs)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runs, Is.EqualTo(4));
                Assert.That(exception.Message, Is.EqualTo("Failure 4"));
                Assert.That(exception.StackTrace, Does.Contain(nameof(Fail)));
            }
        }

        [Test]
        public void Execute_WhenRetryOnRejectsTheException_DoesNotRetry()
        {
            var runs = 0;

            Assert.Throws<InvalidOperationException>(() => ImmediateRetries.Execute(_ =>
            {
                ++runs;
                throw new InvalidOperationException();
            }, retryOn: error => error is IOException));

            Assert.That(runs, Is.EqualTo(1));
        }

        [Test]
        public void Execute_WhenCanceledWhileWaiting_EndsTheWaitPromptly()
        {
            var retry = RetryStrategies.Uniform(TimeSpan.FromMinutes(1));
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            var timer = Stopwatch.StartNew();
            Assert.Catch<OperationCanceledException>(() => retry.Execute(_ => throw new IOException(), cancellationToken: cancellationTokenSource.Token));
            timer.Stop();

            Assert.That(timer.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void Execute_WithNullArgument_ThrowsArgumentNullException()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentNullException>(() => ((IRetryStrategy)null!).Execute(_ => { }));
                Assert.Throws<ArgumentNullException>(() => ImmediateRetries.Execute((Action<CancellationToken>)null!));
                Assert.Throws<ArgumentNullException>(() => ImmediateRetries.Execute((Func<CancellationToken, int>)null!));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task FailAsync(int run)
        {
            await Task.Yield();
            throw new IOException($"Failure {run}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Fail(int run)
        {
            throw new IOException($"Failure {run}");
        }
    }
}
