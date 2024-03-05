namespace Kampute.HttpClient.Test
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class SharedDisposableManagerTests
    {
        private class TestDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; } = false;

            public void Dispose() => IsDisposed = true;
        }

        [Test]
        public void Acquire_CreatesInstance_WhenFirstCalled()
        {
            var referenceCount = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            var instance = referenceCount.Acquire();

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.IsDisposed, Is.False);
        }

        [Test]
        public void Release_DisposesInstance_WhenLastReferenceReleased()
        {
            var referenceCount = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            var instance1 = referenceCount.Acquire();
            var instance2 = referenceCount.Acquire();

            var disposed1 = referenceCount.Release(instance1);
            Assert.Multiple(() =>
            {
                Assert.That(disposed1, Is.False);
                Assert.That(instance1.IsDisposed, Is.False);
            });

            var disposed2 = referenceCount.Release(instance2);
            Assert.Multiple(() =>
            {
                Assert.That(disposed2, Is.True);
                Assert.That(instance2.IsDisposed, Is.True);
            });
        }

        [Test]
        public void Release_ThrowsArgumentException_WhenInstanceNotManaged()
        {
            var referenceCount = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            var instance = new TestDisposable(); // Not managed

            Assert.That(() => referenceCount.Release(instance), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void IsManaging_ReturnsTrue_ForManagedInstance()
        {
            var referenceCount = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            var instance = referenceCount.Acquire();

            Assert.That(referenceCount.Is(instance), Is.True);
        }

        [Test]
        public void IsManaging_ReturnsFalse_ForNonManagedInstance()
        {
            var referenceCount = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            referenceCount.Acquire(); // Acquire but ignore the instance
            var anotherInstance = new TestDisposable(); // Not managed

            Assert.That(referenceCount.Is(anotherInstance), Is.False);
        }
    }
}
