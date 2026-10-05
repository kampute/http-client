// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Gives the library one way to read and write the properties of an <see cref="HttpRequestMessage"/> on every target.
    /// </summary>
    /// <remarks>
    /// On modern .NET, <c>HttpRequestMessage.Properties</c> is obsolete and stores its values in <c>HttpRequestMessage.Options</c>, so both views
    /// show the same values. The <c>netstandard2.0</c> build has only <c>Properties</c>.
    /// </remarks>
    internal static class HttpRequestMessagePropertyStore
    {
        /// <summary>
        /// Returns the property bag of the request.
        /// </summary>
        /// <param name="request">The request whose properties to return.</param>
        /// <returns><c>request.Options</c> on modern .NET; <c>request.Properties</c> on the <c>netstandard2.0</c> build.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDictionary<string, object?> GetPropertyBag(this HttpRequestMessage request)
        {
#if !NETSTANDARD2_0
            return request.Options;
#else
            return request.Properties;
#endif
        }
    }
}
