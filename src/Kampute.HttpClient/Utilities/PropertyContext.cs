namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Manages properties within specific contexts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class facilitates the management of contextual properties, which are key-value pairs associated with distinct operational contexts,
    /// such as HTTP requests, database transactions, or other scenarios requiring contextual data preservation.
    /// </para>
    /// <para>
    /// When enumerating the collection, properties are presented from the outermost to the innermost scope, ensuring that properties in outer
    /// scopes are encountered before those in nested scopes. This ordering reflects the hierarchical relationship and scope precedence, where
    /// properties defined in outer scopes may be overridden by those in inner scopes.
    /// </para>
    /// <para>
    /// This class is thread-safe and can be utilized reliably in concurrent and asynchronous operations, ensuring that contextual properties
    /// remain accessible and intact across the lifespan of a context.
    /// </para>
    /// </remarks>
    public class PropertyContext : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly AsyncLocal<Scope?> _activeScope = new();

        /// <summary>
        /// Gets the scope of current context, if any.
        /// </summary>
        /// <value>The scope of current context. It is <c>null</c>> if there is no active scope.</value>
        public Scope? Current => _activeScope.Value;

        /// <summary>
        /// Initiates a new scope within the current context, incorporating the specified properties.
        /// </summary>
        /// <param name="properties">The properties to include in the new scope.</param>
        /// <returns>A new instance of the <see cref="Scope"/> class, containing the specified properties.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="properties"/> is <c>null</c>>.</exception>
        public virtual Scope BeginScope(IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            lock (_activeScope)
            {
                var scope = new Scope(this, _activeScope.Value, properties);
                _activeScope.Value = scope;
                return scope;
            }
        }

        /// <summary>
        /// Ends the specified scope and removes it from the current context.
        /// </summary>
        /// <param name="scope">The scope to be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scope"/> is <c>null</c>>.</exception>
        protected virtual void EndScope(Scope scope)
        {
            if (scope is null)
                throw new ArgumentNullException(nameof(scope));

            lock (_activeScope)
            {
                if (_activeScope.Value == scope)
                    _activeScope.Value = scope.Parent;
            }
        }

        /// <summary>
        /// Merges properties from the current context into the provided dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary into which properties should be merged.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method delegates to the active scope to merge its properties into the provided dictionary, overwriting any existing
        /// entries with the same keys. This policy ensures that the most specific settings from inner scopes override those from outer
        /// scopes, reflecting the current state of the context hierarchy.
        /// </remarks>
        public virtual void MergeInto(IDictionary<string, object> dictionary)
        {
            _activeScope.Value?.MergeInto(dictionary);
        }

        /// <summary>
        /// Merges properties from the current context into the provided dictionary without overwriting existing entries.
        /// </summary>
        /// <param name="dictionary">The dictionary into which properties should be merged.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dictionary"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method delegates to the active scope to merge properties into the provided dictionary only if they are absent, preserving
        /// existing values. It respects the hierarchical structure of scopes without disturbing any specific customizations already defined.
        /// </remarks>
        public virtual void MergeMissingInto(IDictionary<string, object> dictionary)
        {
            _activeScope.Value?.MergeMissingInto(dictionary);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of properties in the current context.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            var current = _activeScope.Value;
            return (current is not null ? current.CumulativeProperties : []).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Represents a scope containing properties used to enhance data management within a specific context.
        /// </summary>
        public sealed class Scope : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Scope"/> class, linking it to its owner and parent scope with specified properties.
            /// </summary>
            /// <param name="owner">The <see cref="PropertyContext"/> that this scope is part of.</param>
            /// <param name="parent">The parent scope of this instance, if any.</param>
            /// <param name="properties">The properties to be associated with this scope.</param>
            internal Scope(PropertyContext owner, Scope? parent, IEnumerable<KeyValuePair<string, object>> properties)
            {
                Owner = owner;
                Parent = parent;
                Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            }

            /// <summary>
            /// Gets the <see cref="PropertyContext"/> that owns this scope.
            /// </summary>
            /// <value>The <see cref="PropertyContext"/> instance that owns this scope.</value>
            public PropertyContext Owner { get; }

            /// <summary>
            /// Gets the parent scope of this instance, if any.
            /// </summary>
            /// <value>The parent scope of this scope. It is <c>null</c>> if there is no parent scope.</value>
            public Scope? Parent { get; }

            /// <summary>
            /// Gets the properties associated with this scope.
            /// </summary>
            /// <value>An enumerable of properties for this scope.</value>
            public IEnumerable<KeyValuePair<string, object>> Properties { get; }

            /// <summary>
            /// Gets the properties associated with this scope and all of its parents.
            /// </summary>
            /// <value>An enumerable of properties for this scope and all of its parents.</value>
            /// <remarks>
            /// <para>
            /// This property provides a single, flattened view of all properties from the current scope back to the outermost parent scope. It combines
            /// properties in a manner that allows easy access to all inherited and locally defined properties.
            /// </para>
            /// <para>
            /// Properties are listed from the outermost to the innermost scope. This ordering ensures that properties defined in outer scopes are
            /// encountered before those defined in nested scopes. This ordering reflects the hierarchical relationship and scope precedence, where
            /// properties defined in outer scopes may be overridden by those in inner scopes.
            /// </para>
            /// </remarks>
            public IEnumerable<KeyValuePair<string, object>> CumulativeProperties => Parent is null ? Properties : Parent.CumulativeProperties.Concat(Properties);

            /// <summary>
            /// Merges properties from this scope and all parent scopes into the provided dictionary.
            /// </summary>
            /// <param name="dictionary">The dictionary into which properties should be merged.</param>
            /// <exception cref="ArgumentNullException">Thrown if <paramref name="dictionary"/> is <c>null</c>>.</exception>
            /// <remarks>
            /// This method adds or updates properties in the dictionary, with inner scope properties overriding outer scope properties where keys match.
            /// This hierarchical merging ensures that the most specific settings from the innermost scopes take precedence.
            /// </remarks>
            public void MergeInto(IDictionary<string, object> dictionary)
            {
                if (dictionary is null)
                    throw new ArgumentNullException(nameof(dictionary));

                Parent?.MergeInto(dictionary);
                foreach (var property in Properties)
                {
                    dictionary[property.Key] = property.Value;
                }
            }

            /// <summary>
            /// Merges properties from this scope and all parent scopes into the provided dictionary without overwriting existing entries.
            /// </summary>
            /// <param name="dictionary">The dictionary into which properties should be merged.</param>
            /// <exception cref="ArgumentNullException">Thrown if <paramref name="dictionary"/> is <c>null</c>.</exception>
            /// <remarks>
            /// This method adds properties only if their keys do not already exist in the dictionary, respecting the hierarchical order of
            /// scopes and preserving existing entries. This non-overriding approach is crucial for maintaining initial settings.
            /// </remarks>
            public void MergeMissingInto(IDictionary<string, object> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                Parent?.MergeMissingInto(dictionary);
                foreach (var property in Properties)
                {
                    if (!dictionary.ContainsKey(property.Key))
                        dictionary[property.Key] = property.Value;
                }
            }

            /// <summary>
            /// Disposes this scope, effectively removing it from the active context.
            /// </summary>
            public void Dispose() => Owner.EndScope(this);
        }
    }
}
