namespace Kampute.HttpClient.Test.Utilities
{
    using Kampute.HttpClient.Utilities;
    using NUnit.Framework;
    using System;
    using System.Threading;

    [TestFixture]
    public class SharedDisposableTests
    {
        private class TestDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; } = false;

            public void Dispose() => IsDisposed = true;
        }

        [Test]
        public void SharedDisposable_DefaultConstructor_CreatesResource_OnFirstReference()
        {
            var sharedDisposable = new SharedDisposable<TestDisposable>();

            using var reference1 = sharedDisposable.AcquireReference();

            Assert.Multiple(() =>
            {
                Assert.That(sharedDisposable.ReferenceCount, Is.EqualTo(1));
                Assert.That(reference1.Instance, Is.Not.Null);
            });
        }

        [Test]
        public void SharedDisposable_FactoryConstructor_CreatesResource_OnFirstReference()
        {
            var factoryInvoked = 0;
            var sharedDisposable = new SharedDisposable<TestDisposable>(() =>
            {
                ++factoryInvoked;
                return new TestDisposable();
            });

            using var reference1 = sharedDisposable.AcquireReference();
            using var reference2 = sharedDisposable.AcquireReference();

            Assert.Multiple(() =>
            {
                Assert.That(sharedDisposable.ReferenceCount, Is.EqualTo(2));
                Assert.That(factoryInvoked, Is.EqualTo(1));
            });
        }

        [Test]
        public void SharedDisposable_DisposesResource_OnLastRelease()
        {
            var instance = new TestDisposable();
            var sharedDisposable = new SharedDisposable<TestDisposable>(() => instance);

            var reference1 = sharedDisposable.AcquireReference();
            var reference2 = sharedDisposable.AcquireReference();

            Assert.That(sharedDisposable.ReferenceCount, Is.EqualTo(2));

            reference1.Dispose();
            Assert.Multiple(() =>
            {
                Assert.That(sharedDisposable.ReferenceCount, Is.EqualTo(1));
                Assert.That(instance.IsDisposed, Is.False);
            });

            reference2.Dispose();
            Assert.Multiple(() =>
            {
                Assert.That(sharedDisposable.ReferenceCount, Is.EqualTo(0));
                Assert.That(instance.IsDisposed, Is.True);
            });
        }

        [Test]
        public void SharedDisposable_MaintainsCorrectReferenceCount_OnConcurrentAcquireAndDispose()
        {
            int numberOfThreads = 100;
            var threads = new Thread[numberOfThreads];
            int createdCount = 0;

            var sharedDisposable = new SharedDisposable<TestDisposable>(() => new TestDisposable());
            for (int i = 0; i < numberOfThreads; i++)
            {
                threads[i] = new Thread(() =>
                {
                    using var reference = sharedDisposable.AcquireReference();
                    Interlocked.Increment(ref createdCount);
                    Thread.Sleep(10);
                });
            }

            foreach (Thread thread in threads)
                thread.Start();

            foreach (Thread thread in threads)
                thread.Join();

            Assert.Multiple(() =>
            {
                Assert.That(sharedDisposable.ReferenceCount, Is.Zero);
                Assert.That(createdCount, Is.EqualTo(numberOfThreads));
            });
        }
    }
}
