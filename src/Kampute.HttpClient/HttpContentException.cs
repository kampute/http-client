// Copyright (C) 2024 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    using System;

    /// <summary>
    /// The exception that is thrown when an invalid or unsupported content is encountered in an HTTP response.
    /// </summary>
    public class HttpContentException : ApplicationException
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
    }
}
