namespace Kampute.HttpClient.Test.Utilities
{
    using Kampute.HttpClient.Utilities;
    using NUnit.Framework;
    using System;
    using System.Threading.Tasks;

    [TestFixture]
    public class AsyncUpdateThrottleTests
    {
        [Test]
        public void Constructor_SetsInitialValue()
        {
            using var synchronizer = new AsyncUpdateThrottle<int>(1);

            Assert.That(synchronizer.Value, Is.EqualTo(1));
        }

        [Test]
        public async Task TryUpdateAsync_UpdatesValue()
        {
            using var synchronizer = new AsyncUpdateThrottle<int>(1);

            var updateResult = await synchronizer.TryUpdateAsync(() => Task.FromResult(42));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updateResult, Is.True);
                Assert.That(synchronizer.Value, Is.EqualTo(42));
            }
        }

        [Test]
        public async Task TryUpdateAsync_DoesNotUpdateIfAnotherUpdateHasCompleted()
        {
            var synchronizer = new AsyncUpdateThrottle<int>(1);

            var results = await Task.WhenAll
            (
                synchronizer.TryUpdateAsync(async () =>
                {
                    await Task.Delay(100);
                    return 2;
                }),
                synchronizer.TryUpdateAsync(async () =>
                {
                    await Task.Delay(10);
                    return 3;
                })
            );

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results[0], Is.True);
                Assert.That(results[1], Is.False);
                Assert.That(synchronizer.Value, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task TryUpdateAsync_StartedAfterAnotherUpdateCompleted_UpdatesValue()
        {
            using var synchronizer = new AsyncUpdateThrottle<int>(1);

            var firstResult = await synchronizer.TryUpdateAsync(() => Task.FromResult(2));
            var secondResult = await synchronizer.TryUpdateAsync(() => Task.FromResult(3));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstResult, Is.True);
                Assert.That(secondResult, Is.True);
                Assert.That(synchronizer.Value, Is.EqualTo(3));
            }
        }

        [Test]
        public async Task TryUpdateAsync_StartedWhileAnotherUpdateIsRunning_DoesNotUpdateValue()
        {
            using var synchronizer = new AsyncUpdateThrottle<int>(1);
            var firstUpdateGate = new TaskCompletionSource<bool>();

            var firstUpdate = synchronizer.TryUpdateAsync(async () =>
            {
                await firstUpdateGate.Task;
                return 2;
            });
            var secondUpdate = synchronizer.TryUpdateAsync(() => Task.FromResult(3));
            firstUpdateGate.SetResult(true);

            var results = await Task.WhenAll(firstUpdate, secondUpdate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results[0], Is.True);
                Assert.That(results[1], Is.False);
                Assert.That(synchronizer.Value, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task LastUpdateTime_KeepsFullPrecision()
        {
            using var synchronizer = new AsyncUpdateThrottle<int>(1);
            await synchronizer.TryUpdateAsync(() => Task.FromResult(2));

            var before = DateTimeOffset.UtcNow;
            await synchronizer.TryUpdateAsync(() => Task.FromResult(3));
            var after = DateTimeOffset.UtcNow;

            Assert.That(synchronizer.LastUpdateTime, Is.InRange(before, after));
        }
    }
}
