namespace Kampute.HttpClient.Test
{
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net.Http;

    [TestFixture]
    public class HttpRequestMessageExtensionsTests
    {
        [Test]
        public void Clone_WithNonStreamContent_CreatesCopy()
        {
            using var originalRequest = new HttpRequestMessage(HttpMethod.Get, "http://test.com")
            {
                Content = new StringContent("Test content"),
                Version = new Version(2, 0)
            };
            originalRequest.Headers.Add("Test-Header", "HeaderValue");

            using var clonedRequest = originalRequest.Clone();

            Assert.That(clonedRequest, Is.Not.Null);
            Assert.That(clonedRequest, Is.Not.SameAs(originalRequest));
            Assert.Multiple(() =>
            {
                Assert.That(clonedRequest.RequestUri, Is.EqualTo(originalRequest.RequestUri));
                Assert.That(clonedRequest.Version, Is.EqualTo(originalRequest.Version));
                Assert.That(clonedRequest.Headers.Contains("Test-Header"), Is.True);
                Assert.That(clonedRequest.Content, Is.SameAs(originalRequest.Content));
                Assert.That(clonedRequest.GetCloneGeneration(), Is.EqualTo(1));
            });
        }

        [Test]
        public void Clone_WithStreamContent_ThrowsInvalidOperationException()
        {
            using var requestWithStreamContent = new HttpRequestMessage
            {
                Content = new StreamContent(new MemoryStream())
            };

            Assert.That(requestWithStreamContent.Clone, Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void IsClonable_WithNonStreamContent_ReturnsTrue()
        {
            var request = new HttpRequestMessage();

            Assert.That(request.CanClone(), Is.True);
        }

        [Test]
        public void IsClonable_WithStreamContent_ReturnsFalse()
        {
            using var request = new HttpRequestMessage
            {
                Content = new StreamContent(new MemoryStream())
            };

            Assert.That(request.CanClone(), Is.False);
        }

        [Test]
        public void IsCloned_WhenCloned_ReturnsTrue()
        {
            using var originalRequest = new HttpRequestMessage();
            using var clonedRequest = originalRequest.Clone();

            Assert.That(clonedRequest.IsCloned(), Is.True);
        }

        [Test]
        public void IsCloned_WhenNotCloned_ReturnsFalse()
        {
            using var request = new HttpRequestMessage();

            Assert.That(request.IsCloned(), Is.False);
        }

        [Test]
        public void GetCloneGeneration_WhenClonedMultipleTimes_ReturnsCorrectCount()
        {
            using var originalRequest = new HttpRequestMessage();
            using var firstGenerationClone = originalRequest.Clone();
            using var secondGenerationClone = firstGenerationClone.Clone();

            Assert.Multiple(() =>
            {
                Assert.That(originalRequest.GetCloneGeneration(), Is.EqualTo(0));
                Assert.That(firstGenerationClone.GetCloneGeneration(), Is.EqualTo(1));
                Assert.That(secondGenerationClone.GetCloneGeneration(), Is.EqualTo(2));
            });
        }
    }
}
