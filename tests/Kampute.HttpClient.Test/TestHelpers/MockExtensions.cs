namespace Kampute.HttpClient.Test.TestHelpers
{
    using Moq;
    using Moq.Protected;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class MockExtensions
    {
        public static void MockHttpResponse(this Mock<HttpMessageHandler> mockMessageHandler, Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            if (mockMessageHandler is null)
                throw new ArgumentNullException(nameof(mockMessageHandler));
            if (responseFactory is null)
                throw new ArgumentNullException(nameof(responseFactory));

            mockMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>
                (
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync
                (
                    (HttpRequestMessage request, CancellationToken _)
                        => responseFactory(request) ?? throw new InvalidOperationException($"No response for the '{request.Method} {request.RequestUri}' request is provided.")
                )
                .Verifiable();
        }

        public static void MockHttpResponse(this Mock<HttpMessageHandler> mockMessageHandler, HttpStatusCode statusCode, HttpContent? content = null)
        {
            if (mockMessageHandler is null)
                throw new ArgumentNullException(nameof(mockMessageHandler));

            mockMessageHandler.MockHttpResponse(_ => new HttpResponseMessage(statusCode) { Content = content });
        }
    }
}
