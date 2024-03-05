namespace Kampute.HttpClient.Test.TestHelpers
{
    using Kampute.HttpClient;
    using Kampute.HttpClient.Interfaces;
    using System.Net;

    internal class TestErrorResponse : IHttpErrorResponse
    {
        public string Message { get; }

        public TestErrorResponse(string message) => Message = message;

        public override string ToString() => Message;

        public HttpResponseException ToException(HttpStatusCode statusCode) => new(statusCode, Message);
    }
}