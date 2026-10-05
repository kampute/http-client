namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.TestSupport;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// The client stores its request properties through <see cref="HttpRequestMessage.Options"/> on modern .NET. These tests show that the values
    /// are also visible through the obsolete <c>HttpRequestMessage.Properties</c>, which code written for earlier versions still reads.
    /// </summary>
    [TestFixture]
    public class RequestPropertyStorageTests
    {
        [Test]
        public async Task ClientProperties_AreVisibleThroughOptionsAndProperties()
        {
            var handler = new Mock<HttpMessageHandler>();
            var fromOptions = default(Guid);
            var fromProperties = default(object);
            var scopedFromProperties = default(object);
            handler.MockHttpResponse(request =>
            {
                request.Options.TryGetValue(new HttpRequestOptionsKey<Guid>(HttpRequestMessagePropertyKeys.TransactionId), out fromOptions);
#pragma warning disable CS0618 // Properties is obsolete; reading it is the point of the test.
                request.Properties.TryGetValue(HttpRequestMessagePropertyKeys.TransactionId, out fromProperties);
                request.Properties.TryGetValue("Scoped", out scopedFromProperties);
#pragma warning restore CS0618
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });
            using var client = new HttpRestClient(new HttpClient(handler.Object))
            {
                BaseAddress = new Uri("http://api.test.com"),
            };

            using (client.BeginPropertyScope([new("Scoped", "value")]))
            {
                using var _ = await client.SendAsync(HttpMethod.Get, "/resource");
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromOptions, Is.Not.EqualTo(Guid.Empty));
                Assert.That(fromProperties, Is.EqualTo(fromOptions));
                Assert.That(scopedFromProperties, Is.EqualTo("value"));
            }
        }

        [Test]
        public void ValueSetThroughProperties_IsVisibleThroughOptions()
        {
            using var request = new HttpRequestMessage();

#pragma warning disable CS0618 // Properties is obsolete; writing it is the point of the test.
            request.Properties["key"] = 42;
#pragma warning restore CS0618

            Assert.That(request.Options.TryGetValue(new HttpRequestOptionsKey<int>("key"), out var value) && value == 42, Is.True);
        }

        [Test]
        public void CloneOfRequest_CopiesOptionsAndCountsGenerations()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://api.test.com/resource");
            request.Options.Set(new HttpRequestOptionsKey<string>("key"), "value");

            using var clone = request.Clone();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(clone.Options.TryGetValue(new HttpRequestOptionsKey<string>("key"), out var value) ? value : null, Is.EqualTo("value"));
                Assert.That(clone.GetCloneGeneration(), Is.EqualTo(1));
                Assert.That(clone.IsCloned(), Is.True);
                Assert.That(request.IsCloned(), Is.False);
            }
        }
    }
}
