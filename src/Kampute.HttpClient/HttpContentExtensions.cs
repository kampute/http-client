// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// Provides extension methods for <see cref="HttpContent"/> to enhance functionality related to HTTP content processing.
    /// </summary>
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Attempts to find the character encoding from the <see cref="HttpContent"/> headers.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance to extract the character encoding from.</param>
        /// <returns>The <see cref="Encoding"/> specified in the content's headers if the charset is recognized; otherwise, <c>null</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if the charset specified in the content's headers is not recognized.</exception>
        /// <remarks>
        /// This method inspects the 'CharSet' value in the content type header of the <see cref="HttpContent"/>. If the charset is specified
        /// and recognized, it returns the corresponding <see cref="Encoding"/>. If the charset is not specified, the method returns <c>null</c>, 
        /// indicating that the encoding could not be determined. An <see cref="ArgumentException"/> is thrown if the charset is specified but 
        /// not supported by the system.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Encoding? FindCharacterEncoding(this HttpContent content)
        {
            return content.Headers.ContentType?.CharSet is string charSet ? Encoding.GetEncoding(charSet) : null;
        }

        /// <summary>
        /// Determines whether the <see cref="HttpContent"/> instance can be reused.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance to check for re-usability.</param>
        /// <returns><c>true</c> if the content is not of type <see cref="StreamContent"/> and thus considered reusable; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Stream-based content (<see cref="StreamContent"/>) is not reusable because the underlying stream can be consumed once. This method provides 
        /// a quick check to determine if the content is not stream-based and thus potentially reusable across multiple requests or operations.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReusable(this HttpContent content)
        {
            return content is not StreamContent;
        }
    }
}
