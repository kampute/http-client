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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(retryAfterTime, Is.EqualTo(expectedDate).Within(TimeSpan.FromSeconds(1)));
            }
        }

        [Test]
        public void TryExtractRetryAfterTime_WithValidDelta_ReturnsTrueAndTime()
        {
            var delta = TimeSpan.FromHours(1);
            _headers.RetryAfter = new RetryConditionHeaderValue(delta);

            var result = _headers.TryExtractRetryAfterTime(out var retryAfterTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(retryAfterTime, Is.EqualTo(DateTimeOffset.UtcNow.Add(delta)).Within(TimeSpan.FromSeconds(1)));
            }
        }

        [Test]
        public void TryExtractRetryAfterTime_WithoutRetryAfterHeader_ReturnsFalse()
        {
            var result = _headers.TryExtractRetryAfterTime(out var retryAfterTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(retryAfterTime, Is.Null);
            }
        }

        [Test]
        public void TryExtractRateLimitResetTime_WithValidUnixTimestampHeader_ReturnsTrueAndTime()
        {
            var expectedTime = DateTimeOffset.UtcNow.AddHours(1);
            _headers.Add("x-rate-limit-reset", expectedTime.ToUnixTimeSeconds().ToString());

            var result = _headers.TryExtractRateLimitResetTime(out var resetTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(resetTime, Is.EqualTo(expectedTime).Within(TimeSpan.FromSeconds(1)));
            }
        }

        [Test]
        public void TryExtractRateLimitResetTime_WithOneDayHeader_ReadsSecondsFromNow()
        {
            var expectedTime = DateTimeOffset.UtcNow.AddDays(1);
            _headers.Add("x-ratelimit-reset", "86400");

            var result = _headers.TryExtractRateLimitResetTime(out var resetTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(resetTime, Is.EqualTo(expectedTime).Within(TimeSpan.FromSeconds(1)));
            }
        }

        [Test]
        public void TryExtractRateLimitResetTime_WithValueAboveOneDay_ReadsUnixTime()
        {
            _headers.Add("x-ratelimit-reset", "86401");

            var result = _headers.TryExtractRateLimitResetTime(out var resetTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(resetTime, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(86401)));
            }
        }

        [TestCase("1727000000000")]
        [TestCase("9223372036854775807")]
        [TestCase("-1")]
        public void TryExtractRateLimitResetTime_WithOutOfRangeHeader_ReturnsFalse(string headerValue)
        {
            _headers.Add("x-ratelimit-reset", headerValue);

            var result = _headers.TryExtractRateLimitResetTime(out var resetTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(resetTime, Is.Null);
            }
        }
    }
}
