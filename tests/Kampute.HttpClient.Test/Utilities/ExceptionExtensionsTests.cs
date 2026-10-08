namespace Kampute.HttpClient.Test.Utilities
{
    using Kampute.HttpClient.Utilities;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;

    [TestFixture]
    public class ExceptionExtensionsTests
    {
        [TestCase(SocketError.AddressNotAvailable, ExpectedResult = true)]
        [TestCase(SocketError.ConnectionAborted, ExpectedResult = true)]
        [TestCase(SocketError.ConnectionRefused, ExpectedResult = true)]
        [TestCase(SocketError.ConnectionReset, ExpectedResult = true)]
        [TestCase(SocketError.HostDown, ExpectedResult = true)]
        [TestCase(SocketError.HostNotFound, ExpectedResult = true)]
        [TestCase(SocketError.HostUnreachable, ExpectedResult = true)]
        [TestCase(SocketError.Interrupted, ExpectedResult = true)]
        [TestCase(SocketError.NetworkDown, ExpectedResult = true)]
        [TestCase(SocketError.NetworkReset, ExpectedResult = true)]
        [TestCase(SocketError.NetworkUnreachable, ExpectedResult = true)]
        [TestCase(SocketError.NoBufferSpaceAvailable, ExpectedResult = true)]
        [TestCase(SocketError.NotConnected, ExpectedResult = true)]
        [TestCase(SocketError.OperationAborted, ExpectedResult = true)]
        [TestCase(SocketError.Shutdown, ExpectedResult = true)]
        [TestCase(SocketError.SocketError, ExpectedResult = true)]
        [TestCase(SocketError.TimedOut, ExpectedResult = true)]
        [TestCase(SocketError.TooManyOpenSockets, ExpectedResult = true)]
        [TestCase(SocketError.TryAgain, ExpectedResult = true)]
        [TestCase(SocketError.AccessDenied, ExpectedResult = false)]
        [TestCase(SocketError.AddressAlreadyInUse, ExpectedResult = false)]
        [TestCase(SocketError.MessageSize, ExpectedResult = false)]
        [TestCase(SocketError.NoData, ExpectedResult = false)]
        [TestCase(SocketError.NoRecovery, ExpectedResult = false)]
        public bool IsTransientNetworkError_ForSocketError_ClassifiesByErrorCode(SocketError socketError)
        {
            var error = new HttpRequestException("Connection failure", new SocketException((int)socketError));

            return error.IsTransientNetworkError();
        }

        [TestCase(WebExceptionStatus.ConnectFailure, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.ConnectionClosed, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.KeepAliveFailure, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.NameResolutionFailure, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.PipelineFailure, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.ProxyNameResolutionFailure, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.ReceiveFailure, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.SendFailure, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.Timeout, ExpectedResult = true)]
        [TestCase(WebExceptionStatus.ProtocolError, ExpectedResult = false)]
        [TestCase(WebExceptionStatus.SecureChannelFailure, ExpectedResult = false)]
        [TestCase(WebExceptionStatus.ServerProtocolViolation, ExpectedResult = false)]
        [TestCase(WebExceptionStatus.TrustFailure, ExpectedResult = false)]
        public bool IsTransientNetworkError_ForWebExceptionStatus_ClassifiesByStatus(WebExceptionStatus status)
        {
            var error = new HttpRequestException("Request failure", new WebException("Request failure", status));

            return error.IsTransientNetworkError();
        }

        [Test]
        public void IsTransientNetworkError_ForResponseEndedPrematurely_ReturnsTrue()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(new HttpRequestException("Request failure", new IOException("The response ended prematurely.")).IsTransientNetworkError(), Is.True);
                Assert.That(new HttpRequestException("Request failure", new HttpIOException(HttpRequestError.ResponseEnded)).IsTransientNetworkError(), Is.True);
            }
        }

        [Test]
        public void IsTransientNetworkError_ForHttpProtocolError_ReturnsFalse()
        {
            var error = new HttpRequestException("Request failure", new HttpProtocolException(0x1, "PROTOCOL_ERROR", null));

            Assert.That(error.IsTransientNetworkError(), Is.False);
        }

        [Test]
        public void IsTransientNetworkError_ForTimeout_ReturnsTrue()
        {
            var error = new HttpRequestException("Connection failure", new TimeoutException());

            Assert.That(error.IsTransientNetworkError(), Is.True);
        }

        [Test]
        public void IsTransientNetworkError_WithoutNetworkCause_ReturnsFalse()
        {
            var error = new HttpRequestException("Invalid response", new InvalidOperationException());

            Assert.That(error.IsTransientNetworkError(), Is.False);
        }
    }
}
