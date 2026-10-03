namespace Kampute.HttpClient.NewtonsoftJson.Test
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.TestSupport;
    using Kampute.Retry;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using static Kampute.HttpClient.TestSupport.CompressedContentHelpers;

    [TestFixture]
    public class HttpRestClientJsonExtensionsTests
    {
        private readonly Mock<HttpMessageHandler> _mockMessageHandler = new();
        private HttpRestClient _restClient;

        private Uri AbsoluteUrl(string url)
        {
            return _restClient.BaseAddress is not null
                ? new Uri(_restClient.BaseAddress, url)
                : new Uri(url);
        }

        [SetUp]
        public void Setup()
        {
            var httpClient = new HttpClient(_mockMessageHandler.Object, false);
            _restClient = new HttpRestClient(httpClient)
            {
                BaseAddress = new Uri("http://api.test.com/json"),
            };
            _restClient.UseNewtonsoftJson(TestModel.JsonSettings);
        }

        [TearDown]
        public void Cleanup()
        {
            _restClient.Dispose();
        }

        [Test]
        public async Task PostAsJsonAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "JSON Test" };

            _mockMessageHandler.MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PostAsJsonAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [Test]
        public async Task PutAsJsonAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "JSON Test" };

            _mockMessageHandler.MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PutAsJsonAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [Test]
        public async Task PatchAsJsonAsync_InvokesHttpClientCorrectly()
        {
            var payload = new TestModel { Name = "JSON Test" };

            _mockMessageHandler.MockHttpResponse(request =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Method, Is.EqualTo(HttpMethod.Patch));
                    Assert.That(request.RequestUri, Is.EqualTo(AbsoluteUrl("/echo")));
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = request.Content,
                };
            });

            var result = await _restClient.PatchAsJsonAsync<TestModel>("/echo", payload);

            Assert.That(result, Is.Not.SameAs(payload));
            Assert.That(result, Is.EqualTo(payload));
        }

        [TestCase("gzip", SocketError.HostUnreachable)]
        [TestCase("gzip", SocketError.TimedOut)]
        [TestCase("deflate", SocketError.HostUnreachable)]
        [TestCase("deflate", SocketError.TimedOut)]
        public async Task SendAsync_OnConnectionFailure_WithCompressedJsonContent_RetriesSerializedPayload(string encoding, SocketError socketError)
        {
            var payload = new TestModel { Name = "JSON Test" };
            var maxRetries = 2;
            var attempts = 0;

            _restClient.RetryPolicy = RetryStrategies.Uniform(TimeSpan.Zero).WithMaxAttempts((uint)maxRetries).ToHttpRetryPolicy();

            _mockMessageHandler.MockHttpResponse(request =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo(MediaTypeNames.Application.Json));
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToJsonString()));
                }

                if (attempts <= maxRetries)
                    throw new HttpRequestException("Connection failure", new SocketException((int)socketError));

                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            using var content = new NewtonsoftJsonContent(payload)
            {
                Settings = TestModel.JsonSettings
            };
            using var compressedContent = CompressContent(content, encoding);

            using var response = await _restClient.SendAsync(HttpMethod.Post, "/resource", compressedContent);

            Assert.That(attempts, Is.EqualTo(maxRetries + 1));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public void SendAsync_OnCallerCancellation_WithCompressedJsonContent_DoesNotRetry(string encoding)
        {
            var payload = new TestModel { Name = "JSON Test" };
            var attempts = 0;
            using var cancellationTokenSource = new CancellationTokenSource();

            _restClient.RetryPolicy = RetryStrategies.Uniform(TimeSpan.Zero).WithMaxAttempts(2).ToHttpRetryPolicy();

            _mockMessageHandler.MockHttpResponse((request, cancellationToken) =>
            {
                ++attempts;

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(request.Content, Is.Not.Null);
                    Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                    Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToJsonString()));
                }

                cancellationTokenSource.Cancel();
                throw new OperationCanceledException(cancellationToken);
            });

            using var content = new NewtonsoftJsonContent(payload)
            {
                Settings = TestModel.JsonSettings
            };
            using var compressedContent = CompressContent(content, encoding);

            Assert.ThrowsAsync
            (
                Is.InstanceOf<OperationCanceledException>(),
                async () => await _restClient.SendAsync(HttpMethod.Post, "/resource", compressedContent, cancellationToken: cancellationTokenSource.Token)
            );
            Assert.That(attempts, Is.EqualTo(1));
        }

        [TestCase("gzip")]
        [TestCase("deflate")]
        public async Task SendAsync_OnTimeoutCancellation_WithCompressedJsonContent_UsesRetryPolicy(string encoding)
        {
            var payload = new TestModel { Name = "JSON Test" };
            var mockRetryPolicy = RetryTestHelpers.MockRetryPolicy(1, out var mockRetrySession);

            var attempts = 0;
            using var testHandler = new TestHttpMessageHandler
            {
                ResponseFactory = async (request, cancellationToken) =>
                {
                    ++attempts;

                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(request.Content, Is.Not.Null);
                        Assert.That(request.Content?.Headers.ContentEncoding, Contains.Item(encoding));
                        Assert.That(ReadCompressedContent(request.Content!), Is.EqualTo(payload.ToJsonString()));
                    }

                    if (attempts == 1)
                        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }
            };
            using var timedOutHttpClient = new HttpClient(testHandler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            };

            using var timedOutClient = new HttpRestClient(timedOutHttpClient)
            {
                BaseAddress = new Uri("http://api.test.com"),
            };
            timedOutClient.UseNewtonsoftJson();
            timedOutClient.RetryPolicy = mockRetryPolicy.Object;

            using var content = new NewtonsoftJsonContent(payload)
            {
                Settings = TestModel.JsonSettings
            };
            using var compressedContent = CompressContent(content, encoding);

            using var response = await timedOutClient.SendAsync(HttpMethod.Post, "/resource", compressedContent);

            mockRetryPolicy.Verify(strategy => strategy.CreateSession(It.IsAny<HttpRequestErrorContext>()), Times.Once);
            mockRetrySession.Verify(scheduler => scheduler.WaitAsync(It.IsAny<CancellationToken>()), Times.Once);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(attempts, Is.EqualTo(2));
            }
        }

        [Test]
        public void UseNewtonsoftJson_RegistersOneFormatterAndUpdatesItsSettings()
        {
            var settings = new JsonSerializerSettings();

            var formatter = _restClient.UseNewtonsoftJson(settings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formatter.Settings, Is.SameAs(settings));
                Assert.That(_restClient.ContentFormatters.OfType<NewtonsoftJsonFormatter>().Single(), Is.SameAs(formatter));
            }
        }

        [Test]
        public async Task UseNewtonsoftJson_SettingsApplyToRequestAndResponse()
        {
            _restClient.UseNewtonsoftJson(new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), MissingMemberHandling = MissingMemberHandling.Error });
            var sentBody = default(string);
            _mockMessageHandler.MockHttpResponse(request =>
            {
                sentBody = request.Content!.ReadAsStringAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"name\":\"Echo\"}", Encoding.UTF8, MediaTypeNames.Application.Json),
                };
            });

            var result = await _restClient.PostAsJsonAsync<TestModel>("/echo", new TestModel { Name = "JSON Test" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sentBody, Is.EqualTo("{\"name\":\"JSON Test\"}"));
                Assert.That(result, Is.EqualTo(new TestModel { Name = "Echo" }));
            }
        }

        [Test]
        public async Task PostAsJsonAsync_WithoutRegistration_SendsWithDefaultSettings()
        {
            using var client = new HttpRestClient(new HttpClient(_mockMessageHandler.Object, false))
            {
                BaseAddress = new Uri("http://api.test.com/json"),
            };
            var sentBody = default(string);
            _mockMessageHandler.MockHttpResponse(request =>
            {
                sentBody = request.Content!.ReadAsStringAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            await client.PostAsJsonAsync("/models", new TestModel { Name = "JSON Test" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sentBody, Is.EqualTo(new TestModel { Name = "JSON Test" }.ToJsonString()));
                Assert.That(client.ContentFormatters, Is.Empty);
            }
        }

        [Test]
        public void PostAsJsonAsync_WithNullPayload_ThrowsBeforeReturningTask()
        {
            Assert.Throws<ArgumentNullException>(() => _restClient.PostAsJsonAsync("/models", null!));
        }

        [Test]
        public async Task PostAsJsonAsync_WithBothJsonFormattersRegistered_EachPackageUsesItsOwnFormatter()
        {
            using var client = new HttpRestClient(new HttpClient(_mockMessageHandler.Object, false))
            {
                BaseAddress = new Uri("http://api.test.com/json"),
            };
            Kampute.HttpClient.Json.HttpRestClientJsonExtensions.UseJson(client, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            client.UseNewtonsoftJson(new JsonSerializerSettings { ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() } });
            var sentBodies = new System.Collections.Generic.List<string>();
            _mockMessageHandler.MockHttpResponse(request =>
            {
                sentBodies.Add(request.Content!.ReadAsStringAsync().Result);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });
            var payload = new { FullName = "JSON Test" };

            await Kampute.HttpClient.Json.HttpRestClientJsonExtensions.PostAsJsonAsync(client, "/models", payload);
            await client.PostAsJsonAsync("/models", payload);

            Assert.That(sentBodies, Is.EqualTo(new[] { "{\"fullName\":\"JSON Test\"}", "{\"full_name\":\"JSON Test\"}" }));
        }
    }
}
