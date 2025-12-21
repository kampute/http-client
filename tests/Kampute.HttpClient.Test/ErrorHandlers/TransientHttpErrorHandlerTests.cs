// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Test.ErrorHandlers
{
    using Kampute.HttpClient.ErrorHandlers;
    using Kampute.HttpClient.Test.TestHelpers;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    [TestFixture]
    public class TransientHttpErrorHandlerTests
    {
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _client;

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, disposeHandler: false);
            _client = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
        }

        [TearDown]
        public void Cleanup()
        {
            _client.Dispose();
        }

        [Test]
        public void Constructor_WithNullStatusCodes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TransientHttpErrorHandler((IEnumerable<HttpStatusCode>)null!));
        }

        [Test]
        public void Constructor_WithEmptyStatusCodes_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TransientHttpErrorHandler([]));
        }

        [Test]
        public void Constructor_WithCustomStatusCodes_SetsHandledStatusCodes()
        {
            var customCodes = new[]
            {
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadRequest
            };
            var handler = new TransientHttpErrorHandler(customCodes);

            Assert.That(handler.HandledStatusCodes, Is.EquivalentTo(customCodes));
        }

        [Test]
        [TestCase(HttpStatusCode.RequestTimeout, ExpectedResult = true)]
        [TestCase(HttpStatusCode.BadGateway, ExpectedResult = true)]
        [TestCase(HttpStatusCode.ServiceUnavailable, ExpectedResult = true)]
        [TestCase(HttpStatusCode.GatewayTimeout, ExpectedResult = true)]
        [TestCase(507 /* Insufficient Storage */, ExpectedResult = true)]
        [TestCase(509 /* Bandwidth Limit Exceeded */, ExpectedResult = true)]
        [TestCase(HttpStatusCode.OK, ExpectedResult = false)]
        [TestCase(HttpStatusCode.NotFound, ExpectedResult = false)]
        [TestCase(HttpStatusCode.InternalServerError, ExpectedResult = false)]
        [TestCase(HttpStatusCode.Unauthorized, ExpectedResult = false)]
        [TestCase(HttpStatusCode.Forbidden, ExpectedResult = false)]
        [TestCase(HttpStatusCode.BadRequest, ExpectedResult = false)]
        public bool CanHandle_ForDefaultConfiguration_ReturnsExpectedResults(HttpStatusCode statusCode)
        {
            var handler = new TransientHttpErrorHandler();

            return handler.CanHandle(statusCode);
        }

        [Test]
        public async Task OnTransientHttpError_WithRetryAfterHeader_AsDate_RetriesRequestAfterSpecifiedTime()
        {
            var transientHandler = new TransientHttpErrorHandler();
            _client.ErrorHandlers.Add(transientHandler);

            var retryDelay = TimeSpan.FromMilliseconds(1000);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.Add(retryDelay));
                return response;
            });

            var timer = Stopwatch.StartNew();
            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());
            timer.Stop();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(timer.Elapsed, Is.EqualTo(retryDelay).Within(0.1 * retryDelay));
            }
        }

        [Test]
        public async Task OnTransientHttpError_WithRetryAfterHeader_AsDelta_RetriesRequestAfterSpecifiedDelay()
        {
            var transientHandler = new TransientHttpErrorHandler();
            _client.ErrorHandlers.Add(transientHandler);

            var retryDelay = TimeSpan.FromMilliseconds(1000);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(retryDelay);
                return response;
            });

            var timer = Stopwatch.StartNew();
            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());
            timer.Stop();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(timer.Elapsed, Is.EqualTo(retryDelay).Within(0.1 * retryDelay));
            }
        }

        [Test]
        public async Task OnTransientHttpError_WithoutRetryAfterHeader_RetriesAccordingToDefaultStrategy()
        {
            var transientHandler = new TransientHttpErrorHandler();
            _client.ErrorHandlers.Add(transientHandler);
            _client.BackoffStrategy = BackoffStrategies.Uniform(2, TimeSpan.Zero);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());

            Assert.That(attempts, Is.EqualTo(3));
        }

        [Test]
        public async Task OnTransientHttpError_WithCustomBackoffStrategy_RetriesAccordingToCustomStrategy()
        {
            var transientHandler = new TransientHttpErrorHandler
            {
                OnBackoffStrategy = (ctx, retryAfter) => BackoffStrategies.Uniform(2, TimeSpan.Zero)
            };
            _client.ErrorHandlers.Add(transientHandler);

            var attempts = 0;
            _mockMessageHandler.MockHttpResponse(request =>
            {
                attempts++;

                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            });

            await Assert.ThatAsync(() => _client.SendAsync(HttpMethod.Get, "/unavailable/resource"), Throws.TypeOf<HttpResponseException>());

            Assert.That(attempts, Is.EqualTo(3));
        }
    }
}
