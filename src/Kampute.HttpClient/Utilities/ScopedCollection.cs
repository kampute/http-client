namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Manages items within specific contexts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class facilitates the management of contextual items, which are elements associated with distinct operational contexts,
    /// such as HTTP requests, database transactions, or other scenarios requiring contextual data preservation.
    /// </para>
    /// <para>
    /// When enumerating the collection, items are presented from the outermost to the innermost scope, ensuring that items in outer
    /// scopes are encountered before those in nested scopes. This ordering reflects the hierarchical relationship, where items defined
    /// in outer scopes may be overridden by those in inner scopes.
    /// </para>
    /// <para>
    /// This class is thread-safe and can be utilized reliably in concurrent and asynchronous operations, ensuring that contextual items
    /// remain accessible and intact across the lifespan of a context.
    /// </para>
    /// </remarks>
    public class ScopedCollection<T> : IEnumerable<T>
    {
        private readonly AsyncLocal<Scope?> _activeScope = new();

        /// <summary>
        /// Gets a value indicating whether the current context has an active scope.
        /// </summary>
        /// <value>
        /// <c>true</c> if an active scope is present; otherwise, <c>false</c>.
        /// </value>
        public bool HasActiveScope => _activeScope.Value is not null;

        /// <summary>
        /// Initiates a new scope within the current context, incorporating the specified items.
        /// </summary>
        /// <param name="items">The items to include in the new scope.</param>
        /// <returns>A new instance of the <see cref="Scope"/> class, containing the specified items.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is <c>null</c>.</exception>
        public virtual Scope BeginScope(IEnumerable<T> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            lock (_activeScope)
            {
                var scope = new Scope(this, _activeScope.Value, items);
                _activeScope.Value = scope;
                return scope;
            }
        }

        /// <summary>
        /// Ends the specified scope and removes it from the current context.
        /// </summary>
        /// <param name="scope">The scope to be removed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scope"/> is <c>null</c>.</exception>
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
        /// Traverses items in each scope from the innermost to the outermost, applying an action to each item.
        /// </summary>
        /// <param name="action">The action to perform on each item within the scopes.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Unlike the standard enumeration, which traverses items from outermost to innermost scopes, this method traverses the scopes
        /// starting from the current active scope and moving outward to the parent scopes. This order ensures that actions are performed
        /// on items starting from the most specific (innermost) to the most general (outermost) context.
        /// </remarks>
        public virtual void Traverse(Action<T> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            for (var scope = _activeScope.Value; scope is not null; scope = scope.Parent)
                foreach (var item in scope.Items)
                    action(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of items in the current context.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();

            IEnumerable<T> GetEnumerable()
            {
                var scope = _activeScope.Value;
                if (scope is null)
                    return [];

                var enumerable = scope.Items.AsEnumerable();
                for (var parent = scope.Parent; parent is not null; parent = parent.Parent)
                    enumerable = parent.Items.AsEnumerable().Concat(enumerable);

                return enumerable;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Represents a scope containing items within a specific context.
        /// </summary>
        public sealed class Scope : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Scope"/> class, linking it to its owner and parent scope with specified items.
            /// </summary>
            /// <param name="owner">The <see cref="ScopedCollection{T}"/> that this scope is part of.</param>
            /// <param name="parent">The parent scope of this instance, if any.</param>
            /// <param name="items">The items to be associated with this scope.</param>
            internal Scope(ScopedCollection<T> owner, Scope? parent, IEnumerable<T> items)
            {
                Owner = owner;
                Parent = parent;
                Items = items is IReadOnlyCollection<T> readOnlyCollection ? readOnlyCollection : items.ToList();
            }

            /// <summary>
            /// Gets the <see cref="ScopedCollection{T}"/> that owns this scope.
            /// </summary>
            /// <value>The <see cref="ScopedCollection{T}"/> instance that owns this scope.</value>
            public ScopedCollection<T> Owner { get; }

            /// <summary>
            /// Gets the parent scope of this instance, if any.
            /// </summary>
            /// <value>The parent scope of this scope. It is <c>null</c> if there is no parent scope.</value>
            public Scope? Parent { get; }

            /// <summary>
            /// Gets the read-only collection of items in this scope.
            /// </summary>
            /// <value>The read-only collection of items in this scope.</value>
            public IReadOnlyCollection<T> Items { get; }

            /// <summary>
            /// Disposes this scope, effectively removing it from the active context.
            /// </summary>
            public void Dispose() => Owner.EndScope(this);
        }
    }
}
