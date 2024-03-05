namespace Kampute.HttpClient.Test
{
    using NUnit.Framework;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;

    [TestFixture]
    public class HttpResponseHeadersExtensionsTests
    {
        private HttpResponseHeaders _headers;

        [SetUp]
        public void SetUp()
        {
            using var response = new HttpResponseMessage();
            _headers = response.Headers;
        }

        [Test]
        public void TryExtractRetryAfterTime_WithValidDate_ReturnsTrueAndTime()
        {
            var expectedDate = DateTimeOffset.UtcNow.AddHours(1);
            _headers.RetryAfter = new RetryConditionHeaderValue(expectedDate);

            var result = _headers.TryExtractRetryAfterTime(out var retryAfterTime);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(retryAfterTime, Is.EqualTo(expectedDate).Within(TimeSpan.FromSeconds(1)));
            });
        }

        [Test]
        public void TryExtractRetryAfterTime_WithValidDelta_ReturnsTrueAndTime()
        {
            var delta = TimeSpan.FromHours(1);
            _headers.RetryAfter = new RetryConditionHeaderValue(delta);

            var result = _headers.TryExtractRetryAfterTime(out var retryAfterTime);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(retryAfterTime, Is.EqualTo(DateTimeOffset.UtcNow.Add(delta)).Within(TimeSpan.FromSeconds(1)));
            });
        }

        [Test]
        public void TryExtractRetryAfterTime_WithoutRetryAfterHeader_ReturnsFalse()
        {
            var result = _headers.TryExtractRetryAfterTime(out var retryAfterTime);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(retryAfterTime, Is.Null);
            });
        }

        [Test]
        public void TryExtractRateLimitResetTime_WithValidUnixTimestampHeader_ReturnsTrueAndTime()
        {
            var expectedTime = DateTimeOffset.UtcNow.AddHours(1);
            _headers.Add("x-rate-limit-reset", expectedTime.ToUnixTimeSeconds().ToString());

            var result = _headers.TryExtractRateLimitResetTime(out var resetTime);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(resetTime, Is.EqualTo(expectedTime).Within(TimeSpan.FromSeconds(1)));
            });
        }
    }
}
