using System.Collections;
using System.Collections.Generic;

// A trivial cons list implementation
namespace RxApp
{
    internal static class Stack
    {
        public static bool IsEmpty<T>(this Stack<T> stack)
        {
            return (stack.Head == null) && (stack.Tail == null);
        }

        public static Stack<T> Reverse<T>(this Stack<T> stack)
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

    internal sealed class Stack<T> : IEnumerable<T>
    {
        private static readonly Stack<T> empty = new Stack<T>(default(T), null);

        public static Stack<T> Empty { get { return empty; } }

        private static IEnumerator<T> Enumerate(Stack<T> stack)
        {
            for (;stack.Head != null; stack = stack.Tail)
            {
                yield return stack.Head;
            }
        }

        private readonly Stack<T> tail;
        private readonly T head;

        internal Stack(T head, Stack<T> tail)
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

        public Stack<T> Tail 
        { 
            get
            {
                return tail;
            }
        }

        public Stack<T> Push(T element)
        {
            return new Stack<T>(element, this);
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