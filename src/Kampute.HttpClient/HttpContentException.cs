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
    /// The exception that is thrown when the content of a response cannot be read into the requested type.
    /// </summary>
    /// <remarks>
    /// The content cannot be read when the response has no body, when no registered content formatter reads its media type into the requested type,
    /// or when the formatter fails; in the last case, the exception of the formatter is the inner exception.
    /// </remarks>
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
        /// Gets or sets the content that could not be read.
        /// </summary>
        /// <value>
        /// The content that could not be read, if any.
        /// </value>
        public HttpContent? Content { get; set; }

        /// <summary>
        /// Gets or sets the type into which the content was to be read.
        /// </summary>
        /// <value>
        /// The requested type, if any.
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
