// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// Represents an exception that is thrown when an HTTP request results in a failure status code.
    /// </summary>
    public class HttpResponseException : HttpRequestException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified status code and optional errors.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        public HttpResponseException(HttpStatusCode statusCode)
            : base()
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified error message, status code, and optional errors.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public HttpResponseException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified error message, status code, inner exception, and optional errors.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public HttpResponseException(HttpStatusCode statusCode, string message, Exception? innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP status code associated with the error.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets or sets the detailed errors associated with the error.
        /// </summary>
        public IDictionary<string, string[]>? Errors { get; }

        /// <summary>
        /// Gets or sets the response message associated with the error.
        /// </summary>
        public HttpResponseMessage? ResponseMessage { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder(base.ToString());

            if (ResponseMessage is not null && ResponseMessage.RequestMessage is not null)
            {
                sb.AppendLine();
                sb.Append("Request: ");
                sb.Append(ResponseMessage.RequestMessage.Method);
                sb.Append(' ');
                sb.Append(ResponseMessage.RequestMessage.RequestUri);
            }

            if (Errors is not null && Errors.Count != 0)
            {
                sb.AppendLine();
                sb.Append("Errors:");
                foreach (var error in Errors)
                {
                    sb.AppendLine();
                    sb.Append("  - ");
                    sb.Append(error.Key);
                    sb.Append(':');
                    foreach (var entry in error.Value)
                    {
                        sb.Append(' ');
                        sb.Append(entry);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
