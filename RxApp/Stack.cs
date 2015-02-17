using System.Collections;
using System.Collections.Generic;

// A trivial cons list implementation
namespace RxApp
{
    internal interface IStack<T> : IEnumerable<T> 
    {
        T Head { get; }
        IStack<T> Tail { get; }

        IStack<T> Push(T element);
    }

    internal static class Stack
    {
        public static bool IsEmpty<T>(this IStack<T> stack)
        {
            return (stack.Head == null) && (stack.Tail == null);
        }

        public static IStack<T> Reverse<T>(this IStack<T> stack)
            where T:class
        {
            var retval = Stack<T>.Empty;
            foreach (T val in stack)
            {
                retval = retval.Push(val);
            }
            return retval;
        }
    }

    internal static class Stack<T> where T: class
    {
        private static readonly IStack<T> empty = new Node(null, null);

        public static IStack<T> Empty
        {
            get
            {
                return empty;
            }
        }
            
        private static IEnumerator<T> Enumerate(IStack<T> stack)
        {
            for (;stack.Head != null; stack = stack.Tail)
            {
                yield return stack.Head;
            }
        }

        private sealed class Node : IStack<T> 
        {
            private readonly IStack<T> tail;
            private readonly T head;

            internal Node(T head, IStack<T> tail)
            {
                this.head = head;
                this.tail = tail;
            }

            public T Head
            {
                get
                {
                    return head;
                }
            }

            public IStack<T> Tail 
            { 
                get
                {
                    return tail;
                }
            }

            public IStack<T> Push(T element)
            {
                return new Node(element, this);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return Enumerate(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}