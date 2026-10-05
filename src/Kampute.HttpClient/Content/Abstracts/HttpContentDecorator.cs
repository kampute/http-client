namespace Kampute.HttpClient.Content.Abstracts
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Provides a base class for content that wraps another <see cref="HttpContent"/>.
    /// </summary>
    /// <remarks>
    /// The decorator starts with a copy of the headers of the original content, and disposes the original content when it is disposed, unless it
    /// was created to leave it open.
    /// </remarks>
    public abstract class HttpContentDecorator : HttpContent
    {
        private readonly bool _leaveOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentDecorator"/> class.
        /// </summary>
        /// <param name="content">The content to wrap. It is disposed when this instance is disposed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
        protected HttpContentDecorator(HttpContent content)
            : this(content, leaveOpen: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContentDecorator"/> class, specifying whether the wrapped content is disposed with this instance.
        /// </summary>
        /// <param name="content">The content to wrap.</param>
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
        /// Gets the content that this instance wraps.
        /// </summary>
        /// <value>The wrapped <see cref="HttpContent"/>.</value>
        protected internal HttpContent OriginalContent { get; }

        /// <summary>
        /// Releases the resources of this instance, and disposes the wrapped content unless this instance was created to leave it open.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> when called from <see cref="IDisposable.Dispose"/>; <see langword="false"/> when called from a finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveOpen)
                OriginalContent.Dispose();

            base.Dispose(disposing);
        }
    }
}
