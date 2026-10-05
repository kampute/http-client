namespace Kampute.HttpClient.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Holds items in nested scopes that flow with the asynchronous context of the code that begins them.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <remarks>
    /// <para>
    /// A scope begun with <see cref="BeginScope"/> holds its items until it is disposed. The items are visible to the code that began the scope
    /// and to the asynchronous operations it starts or awaits, but not to unrelated code running at the same time, because the active scope is
    /// kept in an <see cref="AsyncLocal{T}"/>.
    /// </para>
    /// <para>
    /// Enumerating the collection yields the items of the outermost scope first, so that a consumer that applies them in order lets inner scopes
    /// override outer ones. <see cref="Traverse"/> visits the innermost scope first.
    /// </para>
    /// </remarks>
    public class ScopedCollection<T> : IEnumerable<T>
    {
        private readonly AsyncLocal<Scope?> _activeScope = new();

        /// <summary>
        /// Gets a value indicating whether a scope is active in the current asynchronous context.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if an active scope is present; otherwise, <see langword="false"/>.
        /// </value>
        public bool HasActiveScope => _activeScope.Value is not null;

        /// <summary>
        /// Begins a scope with the specified items, nested in the active scope if there is one.
        /// </summary>
        /// <param name="items">The items of the new scope.</param>
        /// <returns>The new <see cref="Scope"/>, which ends when it is disposed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is <see langword="null"/>.</exception>
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
        /// Ends a scope, making its parent the active scope.
        /// </summary>
        /// <param name="scope">The scope to end.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scope"/> is <see langword="null"/>.</exception>
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
        /// Applies an action to the items of the active scopes, starting with the innermost scope.
        /// </summary>
        /// <param name="action">The action to apply to each item.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="action"/> is <see langword="null"/>.</exception>
        public virtual void Traverse(Action<T> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            for (var scope = _activeScope.Value; scope is not null; scope = scope.Parent)
                foreach (var item in scope.Items)
                    action(item);
        }

        /// <summary>
        /// Returns an enumerator over the items of the active scopes, starting with the outermost scope.
        /// </summary>
        /// <returns>An enumerator over the items.</returns>
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
        /// Represents a scope of a <see cref="ScopedCollection{T}"/>, which ends when it is disposed.
        /// </summary>
        public sealed class Scope : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Scope"/> class.
            /// </summary>
            /// <param name="owner">The collection that the scope belongs to.</param>
            /// <param name="parent">The enclosing scope, if any.</param>
            /// <param name="items">The items of the scope.</param>
            internal Scope(ScopedCollection<T> owner, Scope? parent, IEnumerable<T> items)
            {
                Owner = owner;
                Parent = parent;
                Items = items is IReadOnlyCollection<T> readOnlyCollection ? readOnlyCollection : items.ToList();
            }

            /// <summary>
            /// Gets the collection that the scope belongs to.
            /// </summary>
            /// <value>The <see cref="ScopedCollection{T}"/> of this scope.</value>
            public ScopedCollection<T> Owner { get; }

            /// <summary>
            /// Gets the enclosing scope.
            /// </summary>
            /// <value>The scope that was active when this scope began, or <see langword="null"/> if there was none.</value>
            public Scope? Parent { get; }

            /// <summary>
            /// Gets the items of the scope.
            /// </summary>
            /// <value>The items of this scope.</value>
            public IReadOnlyCollection<T> Items { get; }

            /// <summary>
            /// Ends the scope.
            /// </summary>
            public void Dispose() => Owner.EndScope(this);
        }
    }
}
