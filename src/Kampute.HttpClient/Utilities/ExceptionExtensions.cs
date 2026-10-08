// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.IO;
    using System.Net;
#if !NETSTANDARD2_0
    using System.Net.Http;
#endif
    using System.Net.Sockets;

    /// <summary>
    /// Provides extension methods that classify the exceptions of HTTP requests.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Determines whether an exception reports a network failure that a retry could overcome.
        /// </summary>
        /// <param name="exception">The exception of a failed request.</param>
        /// <returns>
        /// <see langword="true"/> if the innermost exception reports a transient condition; otherwise, <see langword="false"/>. The innermost exception
        /// reports a transient condition if it is:
        /// <list type="bullet">
        ///   <item><description>A <see cref="TimeoutException"/>.</description></item>
        ///   <item>
        ///     <description>
        ///     A <see cref="SocketException"/> for a condition that can clear by itself, such as a refused, reset, or aborted connection, an unreachable
        ///     host or network, a failed host name resolution, or a temporary lack of local sockets, ports, or buffer space.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///     An <see cref="IOException"/> other than an HTTP/2 or HTTP/3 protocol error, which reports that the connection closed before the
        ///     response was complete.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///     A <see cref="WebException"/>, which .NET Framework reports without a <see cref="SocketException"/>, for a failed connection, send, or
        ///     receive, a connection closed early, a failed host name resolution, or a timeout.
        ///     </description>
        ///   </item>
        /// </list>
        /// </returns>
        public static bool IsTransientNetworkError(this Exception exception) => exception.GetBaseException() switch
        {
            TimeoutException => true,
            SocketException e => IsTransient(e.SocketErrorCode),
            WebException e => IsTransient(e.Status),
            IOException e => !IsHttpProtocolError(e),
            _ => false
        };

        /// <summary>
        /// Determines whether a socket error reports a condition that can clear by itself.
        /// </summary>
        /// <param name="socketError">The socket error to classify.</param>
        /// <returns><see langword="true"/> if <paramref name="socketError"/> is transient; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// On Unix, a write to a connection that the peer has closed (<c>EPIPE</c>) is reported as <see cref="SocketError.Shutdown"/>, and a connect
        /// that finds no free ephemeral port (<c>EADDRNOTAVAIL</c>) as <see cref="SocketError.AddressNotAvailable"/>.
        /// </remarks>
        private static bool IsTransient(SocketError socketError) => socketError switch
        {
            SocketError.AccessDenied => false,
            SocketError.AddressAlreadyInUse => false,
            SocketError.AddressFamilyNotSupported => false,
            SocketError.AddressNotAvailable => true,
            SocketError.AlreadyInProgress => false,
            SocketError.ConnectionAborted => true,
            SocketError.ConnectionRefused => true,
            SocketError.ConnectionReset => true,
            SocketError.DestinationAddressRequired => false,
            SocketError.Disconnecting => false,
            SocketError.Fault => false,
            SocketError.HostDown => true,
            SocketError.HostNotFound => true,
            SocketError.HostUnreachable => true,
            SocketError.InProgress => false,
            SocketError.Interrupted => true,
            SocketError.InvalidArgument => false,
            SocketError.IOPending => false,
            SocketError.IsConnected => false,
            SocketError.MessageSize => false,
            SocketError.NetworkDown => true,
            SocketError.NetworkReset => true,
            SocketError.NetworkUnreachable => true,
            SocketError.NoBufferSpaceAvailable => true,
            SocketError.NoData => false,
            SocketError.NoRecovery => false,
            SocketError.NotConnected => true,
            SocketError.NotInitialized => false,
            SocketError.NotSocket => false,
            SocketError.OperationAborted => true,
            SocketError.OperationNotSupported => false,
            SocketError.ProcessLimit => false,
            SocketError.ProtocolFamilyNotSupported => false,
            SocketError.ProtocolNotSupported => false,
            SocketError.ProtocolOption => false,
            SocketError.ProtocolType => false,
            SocketError.Shutdown => true,
            SocketError.SocketError => true,
            SocketError.SocketNotSupported => false,
            SocketError.Success => false,
            SocketError.SystemNotReady => false,
            SocketError.TimedOut => true,
            SocketError.TooManyOpenSockets => true,
            SocketError.TryAgain => true,
            SocketError.TypeNotFound => false,
            SocketError.VersionNotSupported => false,
            SocketError.WouldBlock => false,
            _ => false
        };

        /// <summary>
        /// Determines whether a <see cref="WebException"/> status reports a condition that can clear by itself.
        /// </summary>
        /// <param name="status">The status to classify.</param>
        /// <returns><see langword="true"/> if <paramref name="status"/> is transient; otherwise, <see langword="false"/>.</returns>
        private static bool IsTransient(WebExceptionStatus status) => status switch
        {
            WebExceptionStatus.CacheEntryNotFound => false,
            WebExceptionStatus.ConnectFailure => true,
            WebExceptionStatus.ConnectionClosed => true,
            WebExceptionStatus.KeepAliveFailure => true,
            WebExceptionStatus.MessageLengthLimitExceeded => false,
            WebExceptionStatus.NameResolutionFailure => true,
            WebExceptionStatus.Pending => false,
            WebExceptionStatus.PipelineFailure => true,
            WebExceptionStatus.ProtocolError => false,
            WebExceptionStatus.ProxyNameResolutionFailure => true,
            WebExceptionStatus.ReceiveFailure => true,
            WebExceptionStatus.RequestCanceled => false,
            WebExceptionStatus.RequestProhibitedByCachePolicy => false,
            WebExceptionStatus.RequestProhibitedByProxy => false,
            WebExceptionStatus.SecureChannelFailure => false,
            WebExceptionStatus.SendFailure => true,
            WebExceptionStatus.ServerProtocolViolation => false,
            WebExceptionStatus.Success => false,
            WebExceptionStatus.Timeout => true,
            WebExceptionStatus.TrustFailure => false,
            WebExceptionStatus.UnknownError => false,
            _ => false
        };

        /// <summary>
        /// Determines whether an I/O exception reports an HTTP/2 or HTTP/3 protocol error.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns><see langword="true"/> if <paramref name="exception"/> is an <c>HttpProtocolException</c>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <c>HttpProtocolException</c> exists on .NET 7 and later, so the <c>netstandard2.0</c> build recognizes it by name.
        /// </remarks>
        private static bool IsHttpProtocolError(IOException exception)
        {
#if NETSTANDARD2_0
            return exception.GetType().FullName == "System.Net.Http.HttpProtocolException";
#else
            return exception is HttpProtocolException;
#endif
        }
    }
}
