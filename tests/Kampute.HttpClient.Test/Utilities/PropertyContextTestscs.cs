namespace Kampute.HttpClient.Test.Utilities
{
    using Kampute.HttpClient.Utilities;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [TestFixture]
    public class PropertyContextTests
    {
        [Test]
        public void BeginScope_ReturnsScopeWithProperties()
        {
            var properties = new Dictionary<string, object>
            {
                ["key"] = "value"
            };
            var context = new PropertyContext();

            using var scope = context.BeginScope(properties);

            Assert.That(scope, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(scope.Properties, Is.EqualTo(properties));
                Assert.That(context.Current, Is.SameAs(scope));
            });
        }

        [Test]
        public void EndScope_RemovesScopeFromContext()
        {
            var properties = new Dictionary<string, object>
            {
                ["key"] = "value"
            };
            var context = new PropertyContext();

            var scope = context.BeginScope(properties);
            scope.Dispose();

            Assert.That(context.Current, Is.Null);
        }

        [Test]
        public void MergeInto_MergesPropertiesFromAllScopes()
        {
            var properties1 = new Dictionary<string, object>
            {
                ["key1"] = "value1"
            };
            var properties2 = new Dictionary<string, object>
            {
                ["key2"] = "value2"
            };
            var expectedProperties = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
            var dictionary = new Dictionary<string, object>();
            var context = new PropertyContext();

            using var scope1 = context.BeginScope(properties1);
            using var scope2 = context.BeginScope(properties2);

            context.MergeInto(dictionary);

            Assert.That(dictionary, Is.EqualTo(expectedProperties));
        }

        [Test]
        public void MergeInto_OverridesPropertiesCorrectly()
        {
            var properties1 = new Dictionary<string, object>
            {
                ["key1"] = "value1"
            };
            var properties2 = new Dictionary<string, object>
            {
                ["key2"] = "value2"
            };
            var expectedProperties = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
            var dictionary = new Dictionary<string, object>
            {
                ["key1"] = "initialValue1"
            };
            var context = new PropertyContext();

            using var scope1 = context.BeginScope(properties1);
            using var scope2 = context.BeginScope(properties2);

            context.MergeInto(dictionary);

            Assert.That(dictionary, Is.EqualTo(expectedProperties));
        }

        [Test]
        public void MergeMissingInto_MergesPropertiesFromAllScopes()
        {
            var properties1 = new Dictionary<string, object>
            {
                ["key1"] = "value1"
            };
            var properties2 = new Dictionary<string, object>
            {
                ["key2"] = "value2"
            };
            var expectedProperties = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
            var dictionary = new Dictionary<string, object>();
            var context = new PropertyContext();

            using var scope1 = context.BeginScope(properties1);
            using var scope2 = context.BeginScope(properties2);

            context.MergeMissingInto(dictionary);

            Assert.That(dictionary, Is.EqualTo(expectedProperties));
        }

        [Test]
        public void MergeMissingInto_DoesNotOverrideProperties()
        {
            var properties1 = new Dictionary<string, object>
            {
                ["key1"] = "value1"
            };
            var properties2 = new Dictionary<string, object>
            {
                ["key2"] = "value2"
            };
            var expectedProperties = new Dictionary<string, object>
            {
                ["key1"] = "initialValue1",
                ["key2"] = "value2"
            };
            var dictionary = new Dictionary<string, object>
            {
                ["key1"] = "initialValue1"
            };
            var context = new PropertyContext();

            using var scope1 = context.BeginScope(properties1);
            using var scope2 = context.BeginScope(properties2);

            context.MergeMissingInto(dictionary);

            Assert.That(dictionary, Is.EqualTo(expectedProperties));
        }

        [Test]
        public void GetEnumerator_AccumulatePropertiesFromAllScopes()
        {
            var properties1 = new Dictionary<string, object>
            {
                ["key1"] = "value1"
            };
            var properties2 = new Dictionary<string, object>
            {
                ["key2"] = "value2"
            };
            var expectedProperties = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
            var context = new PropertyContext();

            using var scope1 = context.BeginScope(properties1);
            using var scope2 = context.BeginScope(properties2);

            Assert.That(context, Is.EquivalentTo(expectedProperties));
        }

        [Test]
        public async Task BeginScope_AsyncOperations_IsolatesScopes()
        {
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
            var context = new PropertyContext();

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
