namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Provides the <see cref="HttpClient"/> that instances of <see cref="HttpRestClient"/> share by default.
    /// </summary>
    /// <remarks>
    /// Sharing one <see cref="HttpClient"/> lets all clients reuse the same connections. The <see cref="HttpClient"/> is created when the first
    /// reference is acquired, and disposed when the last reference is released; a later reference creates a new one with <see cref="Factory"/>.
    /// </remarks>
    public static class SharedHttpClient
    {
        private static readonly object _sync = new();
        private static Func<HttpClient>? _factory;
        private static SharedDisposable<HttpClient>? _instance;

        /// <summary>
        /// Acquires a reference to the shared <see cref="HttpClient"/>.
        /// </summary>
        /// <returns>A <see cref="SharedDisposable{T}.Reference"/> to the shared <see cref="HttpClient"/>, which releases it when it is disposed.</returns>
        public static SharedDisposable<HttpClient>.Reference AcquireReference()
        {
            if (_instance is null)
            {
                lock (_sync)
                {
                    _instance ??= new SharedDisposable<HttpClient>(_factory ?? (() => new HttpClient()));
                }
            }

            return _instance.AcquireReference();
        }


        /// <summary>
        /// Gets the number of active references to the shared <see cref="HttpClient"/>.
        /// </summary>
        /// <value>The number of active references.</value>
        public static int ReferenceCount => _instance is not null ? _instance.ReferenceCount : 0;

        /// <summary>
        /// Gets or sets the function that creates the shared <see cref="HttpClient"/>.
        /// </summary>
        /// <value>
        /// The function that creates the shared <see cref="HttpClient"/>, or <see langword="null"/> to create one with its parameterless constructor.
        /// </value>
        /// <exception cref="InvalidOperationException">Thrown if the value is set after the first reference has been acquired.</exception>
        /// <remarks>
        /// Set this property at application startup, before any <see cref="HttpRestClient"/> is created with the shared <see cref="HttpClient"/>,
        /// to configure its handler, proxy, or timeout.
        /// </remarks>
        public static Func<HttpClient>? Factory
        {
            get => _factory;
            set
            {
                lock (_sync)
                {
                    if (_instance is not null)
                        throw new InvalidOperationException("Cannot change the factory once the HttpClient instance has been created.");

                    _factory = value;
                }
            }
        }
    }
}
