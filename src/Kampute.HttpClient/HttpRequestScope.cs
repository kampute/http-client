namespace Kampute.HttpClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Collects headers and request properties, and applies them to the requests that a client sends within <see cref="PerformAsync(Func{HttpRestClient, Task})"/>.
    /// </summary>
    /// <remarks>
    /// Create an instance with <see cref="HttpRestClientExtensions.WithScope"/>. The headers and properties apply only to the requests of the
    /// action that <see cref="PerformAsync(Func{HttpRestClient, Task})"/> runs, as with <see cref="HttpRestClient.BeginHeaderScope"/> and
    /// <see cref="HttpRestClient.BeginPropertyScope"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var report = await client
    ///     .WithScope()
    ///     .SetHeader("Accept", MediaTypeNames.Text.Csv)
    ///     .PerformAsync(scopedClient => scopedClient.GetAsStringAsync("reports/daily"));
    /// </code>
    /// </example>
    public sealed class HttpRequestScope
    {
        private Dictionary<string, string?>? _headers;
        private Dictionary<string, object?>? _properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestScope"/> class.
        /// </summary>
        /// <param name="client">The client whose requests the scope applies to.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> is <see langword="null"/>.</exception>
        public HttpRequestScope(HttpRestClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Gets the client whose requests the scope applies to.
        /// </summary>
        /// <value>The <see cref="HttpRestClient"/> of this scope.</value>
        public HttpRestClient Client { get; }

        /// <summary>
        /// Gets the headers of this scope.
        /// </summary>
        /// <value>
        /// The headers to set, and, with a <see langword="null"/> value, the headers to remove.
        /// </value>
        public IReadOnlyCollection<KeyValuePair<string, string?>> Headers => _headers ?? [];

        /// <summary>
        /// Gets the request properties of this scope.
        /// </summary>
        /// <value>
        /// The properties to set, and, with a <see langword="null"/> value, the properties to remove.
        /// </value>
        public IReadOnlyCollection<KeyValuePair<string, object?>> Properties => _properties ?? [];

        /// <summary>
        /// Sets a header of the requests sent within this scope, replacing any default value.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The value of the header.</param>
        /// <returns>The same <see cref="HttpRequestScope"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> argument is <see langword="null"/>.</exception>
        public HttpRequestScope SetHeader(string name, string value)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            _headers ??= [];
            _headers[name] = value;
            return this;
        }

        /// <summary>
        /// Removes a header from the requests sent within this scope, including a default one.
        /// </summary>
        /// <param name="name">The header name to remove.</param>
        /// <returns>The same <see cref="HttpRequestScope"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> argument is <see langword="null"/>.</exception>
        public HttpRequestScope UnsetHeader(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            _headers ??= [];
            _headers[name] = null;
            return this;
        }

        /// <summary>
        /// Sets a request property of the requests sent within this scope.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The same <see cref="HttpRequestScope"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> argument is <see langword="null"/>.</exception>
        public HttpRequestScope SetProperty(string name, object value)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            _properties ??= [];
            _properties[name] = value;
            return this;
        }

        /// <summary>
        /// Removes a request property from the requests sent within this scope.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>The same <see cref="HttpRequestScope"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> argument is <see langword="null"/>.</exception>
        public HttpRequestScope UnsetProperty(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            _properties ??= [];
            _properties[name] = null;
            return this;
        }

        /// <summary>
        /// Runs an asynchronous action whose requests get the headers and properties of this scope.
        /// </summary>
        /// <param name="scopedAction">The action to run. It receives the client of this scope.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scopedAction"/> is <see langword="null"/>.</exception>
        public Task PerformAsync(Func<HttpRestClient, Task> scopedAction)
        {
            if (scopedAction is null)
                throw new ArgumentNullException(nameof(scopedAction));

            return PerformCoreAsync(scopedAction);
        }

        /// <summary>
        /// Executes the action of <see cref="PerformAsync(Func{HttpRestClient, Task})"/> within the scope, after its arguments have been validated.
        /// </summary>
        /// <param name="scopedAction">The asynchronous action to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task PerformCoreAsync(Func<HttpRestClient, Task> scopedAction)
        {
            using var propertyScope = _properties is not null ? Client.BeginPropertyScope(_properties) : null;
            using var headerScope = _headers is not null ? Client.BeginHeaderScope(_headers) : null;
            await scopedAction(Client).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs an asynchronous function whose requests get the headers and properties of this scope, and returns its result.
        /// </summary>
        /// <typeparam name="T">The type of the result of the function.</typeparam>
        /// <param name="scopedFunction">The function to run. It receives the client of this scope.</param>
        /// <returns>A task representing the asynchronous operation with a result of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scopedFunction"/> is <see langword="null"/>.</exception>
        public Task<T> PerformAsync<T>(Func<HttpRestClient, Task<T>> scopedFunction)
        {
            if (scopedFunction is null)
                throw new ArgumentNullException(nameof(scopedFunction));

            return PerformCoreAsync(scopedFunction);
        }

        /// <summary>
        /// Executes the function of <see cref="PerformAsync{T}(Func{HttpRestClient, Task{T}})"/> within the scope, after its arguments have been validated.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the scoped function.</typeparam>
        /// <param name="scopedFunction">The asynchronous function to execute.</param>
        /// <returns>A task representing the asynchronous operation with a result of type <typeparamref name="T"/>.</returns>
        private async Task<T> PerformCoreAsync<T>(Func<HttpRestClient, Task<T>> scopedFunction)
        {
            using var propertyScope = _properties is not null ? Client.BeginPropertyScope(_properties) : null;
            using var headerScope = _headers is not null ? Client.BeginHeaderScope(_headers) : null;
            return await scopedFunction(Client).ConfigureAwait(false);
        }
    }
}
