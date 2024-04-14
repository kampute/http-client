namespace Kampute.HttpClient.Content.Abstracts
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Serves as a base class for decorating <see cref="HttpContent"/> instances. 
    /// </summary>
    /// <remarks>
    /// This class provides common functionality such as copying headers from the original content and managing the lifecycle of the wrapped content.
    /// </remarks>
    public abstract class HttpContentDecorator : HttpContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentDecorator"/> class.
        /// </summary>
        /// <param name="content">The HTTP content to decorate. This content will be disposed when this decorator instance is disposed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <c>null</c>.</exception>
        protected HttpContentDecorator(HttpContent content)
        {
            OriginalContent = content ?? throw new ArgumentNullException(nameof(content));
            foreach (var header in content.Headers)
                Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        /// <summary>
        /// Gets the original HTTP content that this instance decorates.
        /// </summary>
        protected internal HttpContent OriginalContent { get; }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="HttpContent"/> and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                OriginalContent.Dispose();

            base.Dispose(disposing);
        }
    }
}
