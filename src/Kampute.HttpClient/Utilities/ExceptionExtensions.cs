// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Net.Sockets;

    /// <summary>
    /// Provides extension methods for <see cref="Exception"/> to enhance functionality related to HTTP request execution.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Determines whether a given exception can be considered as a transient network issue for the purposes of retrying an HTTP request.
        /// </summary>
        /// <param name="exception">The exception encountered during the HTTP request execution.</param>
        /// <returns><see langword="true"/> if the error can be considered as a transient network issue and might warrant a retry attempt; <see langword="false"/> otherwise.</returns>
        public static bool IsTransientNetworkError(this Exception exception) => exception.GetBaseException() switch
        {
            TimeoutException => true,
            SocketException e => e.SocketErrorCode switch
            {
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
                SocketError.NoBufferSpaceAvailable => false,
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
                SocketError.Shutdown => false,
                SocketError.SocketError => true,
                SocketError.SocketNotSupported => false,
                SocketError.Success => false,
                SocketError.SystemNotReady => false,
                SocketError.TimedOut => true,
                SocketError.TooManyOpenSockets => false,
                SocketError.TryAgain => true,
                SocketError.TypeNotFound => false,
                SocketError.VersionNotSupported => false,
                SocketError.WouldBlock => false,
                _ => false
            },
            _ => false
        };
    }
}
