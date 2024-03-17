﻿namespace Kampute.HttpClient.Test.Utilities
{
    using Kampute.HttpClient.Utilities;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class AsyncGuardTests
    {
        [Test]
        public void Constructor_SetsInitialValue()
        {
            using var synchronizer = new AsyncGuard<int>(1);

            Assert.That(synchronizer.Value, Is.EqualTo(1));
        }

        [Test]
        public async Task TryUpdateAsync_UpdatesValue()
        {
            using var synchronizer = new AsyncGuard<int>(1);

            var updateResult = await synchronizer.TryUpdateAsync(() => Task.FromResult(42));

            Assert.Multiple(() =>
            {
                Assert.That(updateResult, Is.True);
                Assert.That(synchronizer.Value, Is.EqualTo(42));
            });
        }

        [Test]
        public async Task TryUpdateAsync_DoesNotUpdateIfAnotherUpdateHasCompleted()
        {
            var synchronizer = new AsyncGuard<int>(1);

            var results = await Task.WhenAll
            (
                synchronizer.TryUpdateAsync(async () =>
                {
                    await Task.Delay(100);
                    return 2;
                }),
                synchronizer.TryUpdateAsync(async () =>
                {
                    await Task.Delay(0);
                    return 3;
                })
            );

            Assert.Multiple(() =>
            {
                Assert.That(results[0], Is.True);
                Assert.That(results[1], Is.False);
                Assert.That(synchronizer.Value, Is.EqualTo(2));
            });
        }
    }
}
