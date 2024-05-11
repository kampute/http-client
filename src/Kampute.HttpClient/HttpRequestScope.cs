namespace Kampute.HttpClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a scope of properties and headers that can be used for <see cref="HttpRestClient"/> requests.
    /// </summary>
    public sealed class HttpRequestScope
    {
        private Dictionary<string, string?>? _headers;
        private Dictionary<string, object?>? _properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestScope"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpRestClient"/> associated with this scope.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="client"/> argument is <c>null</c>>.</exception>
        public HttpRequestScope(HttpRestClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Gets the <see cref="HttpRestClient"/> associated with this scope.
        /// </summary>
        /// <value>The <see cref="HttpRestClient"/> that is used to send HTTP requests within this scope.</value>
        public HttpRestClient Client { get; }

        /// <summary>
        /// Gets the collection of headers that are configured to be applied to the HTTP requests sent within this scope.
        /// </summary>
        /// <value>
        /// The read-only collection of key-value pairs representing the headers to be applied to the HTTP requests sent within this scope.
        /// </value>
        public IReadOnlyCollection<KeyValuePair<string, string?>> Headers => _headers ?? [];

        /// <summary>
        /// Gets the collection of properties that are configured to be applied to the HTTP requests sent within this scope.
        /// </summary>
        /// <value>
        /// The read-only collection of key-value pairs representing the properties to be applied to the HTTP requests sent within this scope.
        /// </value>
        public IReadOnlyCollection<KeyValuePair<string, object?>> Properties => _properties ?? [];

        /// <summary>
        /// Specifies that a header should be used with the specified value for requests sent within this scope.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The value of the header.</param>
        /// <returns>The same <see cref="HttpRequestScope"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> argument is <c>null</c>>.</exception>
        public HttpRequestScope SetHeader(string name, string value)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            _headers ??= [];
            _headers[name] = value;
            return this;
        }

        /// <summary>
        /// Specifies that a header should be removed from requests sent within this scope.
        /// </summary>
        /// <param name="name">The header name to remove.</param>
        /// <returns>The same <see cref="HttpRequestScope"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> argument is <c>null</c>>.</exception>
        public HttpRequestScope UnsetHeader(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            _headers ??= [];
            _headers[name] = null;
            return this;
        }

        /// <summary>
        /// Specifies that a property should be used with the specified value for requests sent within this scope.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The same <see cref="HttpRequestScope"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> argument is <c>null</c>.</exception>
        public HttpRequestScope SetProperty(string name, object value)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            _properties ??= [];
            _properties[name] = value;
            return this;
        }

        /// <summary>
        /// Specifies that a property should be removed from requests sent within this scope.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>The same <see cref="HttpRequestScope"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> argument is <c>null</c>>.</exception>
        public HttpRequestScope UnsetProperty(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            _properties ??= [];
            _properties[name] = null;
            return this;
        }

        /// <summary>
        /// Executes a task within the configured scope, applying all set properties and headers to requests made by the client during the execution of the task.
        /// </summary>
        /// <param name="scopedAction">The asynchronous action to execute, which involves HTTP requests that will include the configured properties and headers.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="scopedAction"/> is <c>null</c>>.</exception>
        public async Task PerformAsync(Func<HttpRestClient, Task> scopedAction)
        {
            if (scopedAction is null)
                throw new ArgumentNullException(nameof(scopedAction));

            using var propertyScope = _properties is not null ? Client.BeginPropertyScope(_properties) : null;
            using var headerScope = _headers is not null ? Client.BeginHeaderScope(_headers) : null;
            await scopedAction(Client);
        }

        /// <summary>
        /// Executes a task within the configured scope, applying all set properties and headers to requests made by the client during the execution of the task, and
        /// returns a result of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the scoped action.</typeparam>
        /// <param name="scopedFunction">The asynchronous function to execute, which involves HTTP requests that will include the configured properties and headers.</param>
        /// <returns>A task representing the asynchronous operation with a result of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="scopedFunction"/> is <c>null</c>>.</exception>
        public async Task<T> PerformAsync<T>(Func<HttpRestClient, Task<T>> scopedFunction)
        {
            if (scopedFunction is null)
                throw new ArgumentNullException(nameof(scopedFunction));

            using var propertyScope = _properties is not null ? Client.BeginPropertyScope(_properties) : null;
            using var headerScope = _headers is not null ? Client.BeginHeaderScope(_headers) : null;
            return await scopedFunction(Client);
        }
    }
}
