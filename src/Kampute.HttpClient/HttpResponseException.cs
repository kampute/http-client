// Copyright (C) 2025 Kampute
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
    /// The exception that is thrown when a request receives an error response that no error handler retries.
    /// </summary>
    public class HttpResponseException : HttpRequestException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the exception.</param>
        public HttpResponseException(HttpStatusCode statusCode)
#if !NETSTANDARD2_0
            : base(null, null, statusCode)
#else
            : base()
#endif
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified status code and error message.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the exception.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public HttpResponseException(HttpStatusCode statusCode, string message)
#if !NETSTANDARD2_0
            : base(message, null, statusCode)
#else
            : base(message)
#endif
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class with the specified status code, error message, and optional inner exception.
        /// </summary>
        /// <param name="statusCode">The HTTP status code associated with the exception.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
        public HttpResponseException(HttpStatusCode statusCode, string message, Exception? innerException)
#if !NETSTANDARD2_0
            : base(message, innerException, statusCode)
#else
            : base(message, innerException)
#endif
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP status code associated with the exception.
        /// </summary>
        /// <value>
        /// The HTTP status code associated with the exception.
        /// </value>
        /// <remarks>
        /// On .NET 10 and later, the inherited <c>HttpRequestException.StatusCode</c> has the same value, so exception filters on
        /// <see cref="HttpRequestException"/> can test the status code of an error response.
        /// </remarks>
#if !NETSTANDARD2_0
        public new HttpStatusCode StatusCode { get; }
#else
        public HttpStatusCode StatusCode { get; }
#endif

        /// <summary>
        /// Gets or sets the validation errors that the error response reports.
        /// </summary>
        /// <value>
        /// The error messages of each invalid field, keyed by the field name, or <see langword="null"/> if there are none. An
        /// <see cref="Interfaces.IHttpErrorResponse"/> can set them from the error body.
        /// </value>
        public IDictionary<string, string[]>? Errors { get; set; }

        /// <summary>
        /// Gets or sets the error response.
        /// </summary>
        /// <value>
        /// The <see cref="HttpResponseMessage"/> of the error, or <see langword="null"/> if there is none.
        /// </value>
        /// <remarks>
        /// <para>
        /// When <see cref="HttpRestClient"/> throws this exception, it has already disposed the response. The status code, reason phrase,
        /// response headers and request message remain readable, but reading the response content throws <see cref="ObjectDisposedException"/>.
        /// </para>
        /// <para>
        /// To use a structured error body, set <see cref="HttpRestClient.ResponseErrorType"/>. The client then deserializes the error body before
        /// disposing the response, and exposes it through <see cref="ResponseObject"/>.
        /// </para>
        /// </remarks>
        public HttpResponseMessage? ResponseMessage { get; set; }

        /// <summary>
        /// Gets or sets the body of the error response, read as <see cref="HttpRestClient.ResponseErrorType"/>.
        /// </summary>
        /// <value>
        /// The object read from the error body, or <see langword="null"/> if <see cref="HttpRestClient.ResponseErrorType"/> is not set or the body
        /// could not be read.
        /// </value>
        public object? ResponseObject { get; set; }

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
