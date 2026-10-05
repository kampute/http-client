// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    /// <summary>
    /// Provides the names of common HTTP authentication schemes, for use in the <c>Authorization</c> header.
    /// </summary>
    /// <example>
    /// <code>
    /// client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthSchemes.Bearer, accessToken);
    /// </code>
    /// </example>
    public static class AuthSchemes
    {
        /// <summary>
        /// The Bearer authentication scheme, defined in RFC 6750, which sends an access token such as one issued by an OAuth 2.0 server.
        /// </summary>
        public const string Bearer = "Bearer";

        /// <summary>
        /// The Basic authentication scheme, defined in RFC 7617, which sends a user ID and password encoded in Base64.
        /// </summary>
        public const string Basic = "Basic";

        /// <summary>
        /// The Digest authentication scheme, defined in RFC 7616, which sends a hash of the credentials instead of the password.
        /// </summary>
        public const string Digest = "Digest";

        /// <summary>
        /// The HTTP Origin-Bound Authentication (HOBA) scheme, defined in RFC 7486, which authenticates with a key pair bound to the origin.
        /// </summary>
        public const string HOBA = "HOBA";

        /// <summary>
        /// The Mutual authentication scheme, defined in RFC 8120, in which the client and the server authenticate each other.
        /// </summary>
        public const string Mutual = "Mutual";

        /// <summary>
        /// The AWS Signature Version 4 scheme, which Amazon Web Services uses to sign API requests.
        /// </summary>
        public const string AWS4HMACSHA256 = "AWS4-HMAC-SHA256";

        /// <summary>
        /// A scheme name commonly used to send an API key in the <c>Authorization</c> header. It is not standardized, so check the name your API expects.
        /// </summary>
        public const string ApiKey = "ApiKey";
    }
}
