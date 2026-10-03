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
        private readonly bool _leaveOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentDecorator"/> class.
        /// </summary>
        /// <param name="content">The HTTP content to decorate. This content will be disposed when this decorator instance is disposed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
        protected HttpContentDecorator(HttpContent content)
            : this(content, leaveOpen: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentDecorator"/> class, specifying whether the decorated content is disposed with this instance.
        /// </summary>
        /// <param name="content">The HTTP content to decorate.</param>
        /// <param name="leaveOpen">
        /// <see langword="true"/> to leave <paramref name="content"/> undisposed when this decorator instance is disposed; <see langword="false"/> to dispose it.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
        protected HttpContentDecorator(HttpContent content, bool leaveOpen)
        {
            _leaveOpen = leaveOpen;
            OriginalContent = content ?? throw new ArgumentNullException(nameof(content));
            foreach (var header in content.Headers)
                Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        /// <summary>
        /// Gets the original HTTP content that this instance decorates.
        /// </summary>
        /// <value>The original <see cref="HttpContent"/> instance.</value>
        protected internal HttpContent OriginalContent { get; }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="HttpContent"/> and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveOpen)
                OriginalContent.Dispose();

            base.Dispose(disposing);
        }
    }
}
