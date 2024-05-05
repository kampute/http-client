namespace Kampute.HttpClient.Test.Utilities
{
    using Kampute.HttpClient.Utilities;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [TestFixture]
    public class ScopedCollectionTests
    {
        [Test]
        public void BeginScope_ReturnsScopeWithProperties()
        {
            var context = new ScopedCollection<int>();
            var items = new[] { 1, 2 };

            using var scope = context.BeginScope(items);

            Assert.Multiple(() =>
            {
                Assert.That(scope, Is.Not.Null);
                Assert.That(context, Is.EqualTo(items));
            });
        }

        [Test]
        public void EndScope_RemovesScopeFromContext()
        {
            var context = new ScopedCollection<int>();
            var items = new[] { 1, 2 };

            var scope = context.BeginScope(items);
            scope.Dispose();

            Assert.That(context, Is.Empty);
        }

        [Test]
        public void GetEnumerator_ColectesItemsFromOuterToInner()
        {
            var context = new ScopedCollection<int>();
            var items1 = new[] { 1, 2 };
            var items2 = new[] { 3, 4 };
            var expectedItems = items1.Concat(items2);

            using var scope1 = context.BeginScope(items1);
            using var scope2 = context.BeginScope(items2);

            Assert.That(context, Is.EqualTo(expectedItems));
        }

        [Test]
        public void Traverse_VisitesItemsFromInnerToOuter()
        {
            var context = new ScopedCollection<int>();
            var items1 = new[] { 1, 2 };
            var items2 = new[] { 3, 4 };
            var expectedItems = items2.Concat(items1);

            using var scope1 = context.BeginScope(items1);
            using var scope2 = context.BeginScope(items2);

            var traversedItems = new List<int>();
            context.Traverse(traversedItems.Add);

            Assert.That(traversedItems, Is.EqualTo(expectedItems));
        }

        [Test]
        public async Task BeginScope_AsyncOperations_IsolatesScopes()
        {
            var context = new ScopedCollection<KeyValuePair<string, object>>();
            var propertiesCommon = new Dictionary<string, object>
            {
                ["key"] = "value"
            };
            var properties1 = new Dictionary<string, object>
            {
                ["key1"] = "value1"
            };
            var properties2 = new Dictionary<string, object>
            {
                ["key2"] = "value2"
            };
            var expectedProperties1 = new Dictionary<string, object>
            {
                ["key"] = "value",
                ["key1"] = "value1"
            };
            var expectedProperties2 = new Dictionary<string, object>
            {
                ["key"] = "value",
                ["key2"] = "value2"
            };

            using var scope = context.BeginScope(propertiesCommon);

            var task1 = Task.Run(async () =>
            {
                using var scope = context.BeginScope(properties1);
                await Task.Delay(100);
                return context.ToDictionary(e => e.Key, e => e.Value);
            });

            var task2 = Task.Run(async () =>
            {
                using var scope = context.BeginScope(properties2);
                await Task.Delay(100);
                var mergedProperties = new Dictionary<string, object>();
                return context.ToDictionary(e => e.Key, e => e.Value);
            });

            var results = await Task.WhenAll(task1, task2);

            Assert.Multiple(() =>
            {
                Assert.That(results[0], Is.EquivalentTo(expectedProperties1));
                Assert.That(results[1], Is.EquivalentTo(expectedProperties2));
            });
        }
    }
}
