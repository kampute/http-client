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
        public void Acquire_WhenFirstCalled_CreatesObject()
        {
            var sharedObj = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            var obj = sharedObj.Acquire();

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.IsDisposed, Is.False);
        }

        [Test]
        public void Release_WhenLastReferenceReleased_DisposesObject()
        {
            var sharedObj = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            var objRef1 = sharedObj.Acquire();
            var objRef2 = sharedObj.Acquire();

            var disposed1 = sharedObj.Release(objRef1);
            Assert.Multiple(() =>
            {
                Assert.That(disposed1, Is.False);
                Assert.That(objRef1.IsDisposed, Is.False);
            });

            var disposed2 = sharedObj.Release(objRef2);
            Assert.Multiple(() =>
            {
                Assert.That(disposed2, Is.True);
                Assert.That(objRef2.IsDisposed, Is.True);
            });
        }

        [Test]
        public void Release_WhenObjectNotManaged_ThrowsArgumentException()
        {
            var sharedObj = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            var anotherObj = new TestDisposable(); // Not managed

            Assert.That(() => sharedObj.Release(anotherObj), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Is_ForManagedObject_ReturnsTrue()
        {
            var sharedObj = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            var obj = sharedObj.Acquire();

            Assert.That(sharedObj.Is(obj), Is.True);
        }

        [Test]
        public void Is_ForNonManagedObject_ReturnsFalse()
        {
            var sharedObj = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());
            sharedObj.Acquire(); // Acquire but ignore the instance
            var anotherObj = new TestDisposable(); // Not managed

            Assert.That(sharedObj.Is(anotherObj), Is.False);
        }

        [Test]
        public void Is_ForNull_ReturnsFalse()
        {
            var sharedObj = new SharedDisposableManager<TestDisposable>(() => new TestDisposable());

            Assert.That(sharedObj.Is(null), Is.False);
        }
    }
}
