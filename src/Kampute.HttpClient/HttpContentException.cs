// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// The exception that is thrown when an invalid or unsupported content is encountered in an HTTP response.
    /// </summary>
    public class HttpContentException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentException"/> class.
        /// </summary>
        public HttpContentException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public HttpContentException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentException"/> class with a specified 
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, 
        /// or a null reference if no inner exception is specified.</param>
        public HttpContentException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets or sets the HTTP content associated with the exception.
        /// </summary>
        /// <value>
        /// The HTTP content associated with the exception, if any.
        /// </value>
        public HttpContent? Content { get; set; }

        /// <summary>
        /// Gets or sets the expected type for deserialization when the exception occurred.
        /// </summary>
        /// <value>
        /// The type expected to be deserialized from the HTTP content, if any.
        /// </value>
        public Type? ObjectType { get; set; }

        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder(base.ToString());

            if (Content is not null && Content.Headers.ContentType is not null)
            {
                sb.AppendLine();
                sb.Append("Content Type: ");
                sb.Append(Content.Headers.ContentType);
            }

            if (ObjectType is not null)
            {
                sb.AppendLine();
                sb.Append("Expected Object Type: ");
                sb.Append(ObjectType.Name);
            }

            return sb.ToString();
        }
    }
}
