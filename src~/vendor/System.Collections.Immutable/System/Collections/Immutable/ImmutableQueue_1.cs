// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable queue.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
    [CollectionBuilder(typeof(ImmutableQueue), nameof(ImmutableQueue.Create))]
    [DebuggerDisplay("IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
    public sealed partial class ImmutableQueue<T> : IImmutableQueue<T>
    {
        /// <summary>
        /// The singleton empty queue.
        /// </summary>
        /// <remarks>
        /// Additional instances representing the empty queue may exist on deserialized instances.
        /// Actually since this queue is a struct, instances don't even apply and there are no singletons.
        /// </remarks>
        private static readonly ImmutableQueue<T> s_EmptyField = new ImmutableQueue<T>(ImmutableStack<T>.Empty, ImmutableStack<T>.Empty);

        /// <summary>
        /// The end of the queue that enqueued elements are pushed onto.
        /// </summary>
        private readonly ImmutableStack<T> _backwards;

        /// <summary>
        /// The end of the queue from which elements are dequeued.
        /// </summary>
        private readonly ImmutableStack<T> _forwards;

        /// <summary>
        /// Backing field for the <see cref="BackwardsReversed"/> property.
        /// </summary>
        private ImmutableStack<T>? _backwardsReversed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableQueue{T}"/> class.
        /// </summary>
        /// <param name="forwards">The forwards stack.</param>
        /// <param name="backwards">The backwards stack.</param>
        internal ImmutableQueue(ImmutableStack<T> forwards, ImmutableStack<T> backwards)
        {
            Debug.Assert(forwards != null);
            Debug.Assert(backwards != null);

            _forwards = forwards;
            _backwards = backwards;
        }

        /// <summary>
        /// Gets the empty queue.
        /// </summary>
        public ImmutableQueue<T> Clear()
        {
            Debug.Assert(s_EmptyField.IsEmpty);
            return Empty;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get
            {
                Debug.Assert(!_forwards.IsEmpty || _backwards.IsEmpty);
                return _forwards.IsEmpty;
            }
        }

        /// <summary>
        /// Gets the empty queue.
        /// </summary>
        public static ImmutableQueue<T> Empty
        {
            get
            {
                Debug.Assert(s_EmptyField.IsEmpty);
                return s_EmptyField;
            }
        }

        /// <summary>
        /// Gets an empty queue.
        /// </summary>
        IImmutableQueue<T> IImmutableQueue<T>.Clear()
        {
            Debug.Assert(s_EmptyField.IsEmpty);
            return this.Clear();
        }

        /// <summary>
        /// Gets the reversed <see cref="_backwards"/> stack.
        /// </summary>
        private ImmutableStack<T> BackwardsReversed
        {
            get
            {
                // Although this is a lazy-init pattern, no lock is required because
                // this instance is immutable otherwise, and a double-assignment from multiple
                // threads is harmless.
                _backwardsReversed ??= _backwards.Reverse();

                Debug.Assert(_backwardsReversed != null);
                return _backwardsReversed;
            }
        }

        /// <summary>
        /// Gets the element at the front of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        public T Peek()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            return _forwards.Peek();
        }

        /// <summary>
        /// Gets a read-only reference to the element at the front of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        public ref readonly T PeekRef()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            return ref _forwards.PeekRef();
        }

        /// <summary>
        /// Adds an element to the back of the queue.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The new queue.
        /// </returns>
        public ImmutableQueue<T> Enqueue(T value)
        {
            if (this.IsEmpty)
            {
                return new ImmutableQueue<T>(ImmutableStack.Create(value), ImmutableStack<T>.Empty);
            }
            else
            {
                return new ImmutableQueue<T>(_forwards, _backwards.Push(value));
            }
        }

        /// <summary>
        /// Adds an element to the back of the queue.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The new queue.
        /// </returns>
        IImmutableQueue<T> IImmutableQueue<T>.Enqueue(T value)
        {
            return this.Enqueue(value);
        }

        /// <summary>
        /// Returns a queue that is missing the front element.
        /// </summary>
        /// <returns>A queue; never <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        public ImmutableQueue<T> Dequeue()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            ImmutableStack<T> f = _forwards.Pop();
            if (!f.IsEmpty)
            {
                return new ImmutableQueue<T>(f, _backwards);
            }
            else if (_backwards.IsEmpty)
            {
                return ImmutableQueue<T>.Empty;
            }
            else
            {
                return new ImmutableQueue<T>(this.BackwardsReversed, ImmutableStack<T>.Empty);
            }
        }

        /// <summary>
        /// Retrieves the item at the head of the queue, and returns a queue with the head element removed.
        /// </summary>
        /// <param name="value">Receives the value from the head of the queue.</param>
        /// <returns>The new queue with the head element removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        public ImmutableQueue<T> Dequeue(out T value)
        {
            value = this.Peek();
            return this.Dequeue();
        }

        /// <summary>
        /// Returns a queue that is missing the front element.
        /// </summary>
        /// <returns>A queue; never <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        IImmutableQueue<T> IImmutableQueue<T>.Dequeue()
        {
            return this.Dequeue();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="Enumerator"/> that can be used to iterate through the collection.
        /// </returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.IsEmpty ?
                Enumerable.Empty<T>().GetEnumerator() :
                new EnumeratorObject(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorObject(this);
        }
    }
}
