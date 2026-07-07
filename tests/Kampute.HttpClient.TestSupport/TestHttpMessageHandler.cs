namespace Kampute.HttpClient.TestSupport
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage? Response { get; set; }

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? ResponseFactory { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ResponseFactory is not null)
                return await ResponseFactory(request, cancellationToken);

            return Response ?? new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
