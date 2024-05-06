namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Provides a singleton-like access to a shared <see cref="HttpClient"/> instance across the application.
    /// </summary>
    /// <remarks>
    /// This static class manages the lifecycle of a single <see cref="HttpClient"/> instance. It ensures efficient resource usage by allowing
    /// the <see cref="HttpClient"/> to be reused throughout the application. The <see cref="HttpClient"/> is managed as a shared disposable resource,
    /// which means it is only disposed when no longer in use by any part of the application.
    /// </remarks>
    public static class SharedHttpClient
    {
        private static readonly object _sync = new();
        private static Func<HttpClient>? _factory;
        private static SharedDisposable<HttpClient>? _instance;

        /// <summary>
        /// Acquires a reference to the shared <see cref="HttpClient"/> instance.
        /// </summary>
        /// <returns>A <see cref="SharedDisposable{T}.Reference"/> that manages the lifetime of the shared <see cref="HttpClient"/>.</returns>
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
        /// Gets the current number of active references to the shared <see cref="HttpClient"/> instance.
        /// </summary>
        /// <value>The number of active references.</value>
        public static int ReferenceCount => _instance is not null ? _instance.ReferenceCount : 0;

        /// <summary>
        /// Gets or sets the factory method used to create the <see cref="HttpClient"/> instance.
        /// </summary>
        /// <value>
        /// A function that returns an <see cref="HttpClient"/> when invoked.
        /// </value>
        /// <exception cref="InvalidOperationException">Thrown if attempting to change the factory after the <see cref="HttpClient"/> instance has been created.</exception>
        /// <remarks>
        /// This property allows for the customization of the <see cref="HttpClient"/> creation process. Changing this property after the <see cref="HttpClient"/>
        /// has been created will throw an <see cref="InvalidOperationException"/> to prevent inconsistent states by modifying the factory method post creation.
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
                }

                _factory = value;
            }
        }
    }
}
