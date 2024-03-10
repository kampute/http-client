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
    /// Represents an exception that is thrown when an HTTP request results in a failure HTTP status code.
    /// </summary>
    public class HttpResponseException : HttpRequestException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the exception.</param>
        public HttpResponseException(HttpStatusCode statusCode)
            : base()
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified status code and error message.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the exception.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public HttpResponseException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified status code, error message, and optional inner exception.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the exception.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a <c>null</c> reference if no inner exception is specified.</param>
        public HttpResponseException(HttpStatusCode statusCode, string message, Exception? innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP status code associated with the exception.
        /// </summary>
        /// <value>
        /// The HTTP status code associated with the exception.
        /// </value>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets or sets the validation errors associated with the exception.
        /// </summary>
        /// <value>
        /// The validation errors associated with the exception, if any. It maps error keys to their corresponding error messages arrays. Can be <c>null</c> if there are no validation errors.
        /// </value>
        public IDictionary<string, string[]>? Errors { get; set; }

        /// <summary>
        /// Gets or sets the response message associated with the exception.
        /// </summary>
        /// <value>
        /// The response message associated with the exception, which may include additional details about the error. Can be <c>null</c> if there is no response message.
        /// </value>
        public HttpResponseMessage? ResponseMessage { get; set; }

        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder(base.ToString());

            if (ResponseMessage is not null)
            {
                if (ResponseMessage.RequestMessage is not null)
                {
                    sb.AppendLine();
                    sb.Append("Request: ");
                    sb.Append(ResponseMessage.RequestMessage.Method);
                    sb.Append(' ');
                    sb.Append(ResponseMessage.RequestMessage.RequestUri);
                }

                sb.AppendLine();
                sb.Append("Response: ");
                sb.Append((int)ResponseMessage.StatusCode);
                sb.Append(' ');
                sb.Append(ResponseMessage.ReasonPhrase);
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
