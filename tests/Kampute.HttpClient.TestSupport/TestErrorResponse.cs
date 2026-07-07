namespace Kampute.HttpClient.TestSupport
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Interfaces;
    using System.Net;

    public class TestErrorResponse : IHttpErrorResponse
    {
        public string Message { get; }

        public TestErrorResponse(string message) => Message = message;

        public override string ToString() => Message;

        public HttpResponseException ToException(HttpStatusCode statusCode) => new(statusCode, Message);
    }
}
