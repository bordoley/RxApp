using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;

namespace RxApp
{   
    public interface IReactiveNotifyCollectionChanged<out T>
    {
        IObservable<NotifyCollectionChangedEventArgs> Changed { get; }
    }

    public interface IRxCollection<T> : ICollection<T>, IReactiveNotifyCollectionChanged<T>
    {
    }

    public interface IRxList<T> : IRxCollection<T>, IList<T>
    {
    }

    public interface IRxReadOnlyCollection<out T> : IReadOnlyCollection<T>, IReactiveNotifyCollectionChanged<T>
    {
    }

    public interface IRxReadOnlyList<out T> : IRxReadOnlyCollection<T>, IReadOnlyList<T>
    {
    }

    public static class RxList 
    {
        public static IRxList<T> Create<T>()
        {
            return new RxListImpl<T>(new List<T>());
        }

        public static IRxList<T> ToReactiveList<T>(this IEnumerable<T> initialValues)
        {
            var backingList = initialValues.ToList();
            return new RxListImpl<T>(backingList);
        }

        public static IRxReadOnlyList<T> ToRxReadOnlyList<T>(this IRxList<T> wrapped)
        {
            return new RxReadOnlyListWrapper<T>(wrapped);
        }

        private sealed class RxListImpl<T> : IRxList<T>
        {
            private readonly Subject<NotifyCollectionChangedEventArgs> _changed = new Subject<NotifyCollectionChangedEventArgs>();
            private readonly IList<T> _inner;

            public RxListImpl(IList<T> backingList)
            {
                _inner = backingList;
            }      

            public int IndexOf(T item)
            {
                return _inner.IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                var ea = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                _inner.Insert(index, item);
                _changed.OnNext(ea);
            }

            public void RemoveAt(int index)
            {
                var item = _inner[index];
                var ea = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                _inner.RemoveAt(index);
                _changed.OnNext(ea);
            }

            public T this[int index]
            {
                get
                {
                    return _inner[index];
                }
                set
                {
                    var ea = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, _inner[index], index);
                    _inner[index] = value;
                    _changed.OnNext(ea);
                }
            }

            public void Add(T item)
            {
                Insert(_inner.Count, item);
            }

            public void Clear()
            {
                var ea = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                _inner.Clear();
                _changed.OnNext(ea);
            }

            public bool Contains(T item)
            {
                return _inner.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _inner.CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                int index = _inner.IndexOf(item);
                if (index < 0) return false;

                var ea = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                _inner.RemoveAt(index);
                _changed.OnNext(ea);
                return true;
            }

            public int Count 
            {
                get { return _inner.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _inner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _inner.GetEnumerator();
            }
                
            public IObservable<NotifyCollectionChangedEventArgs> Changed
            {
                get { return _changed; }
            }
        }

        private sealed class RxReadOnlyListWrapper<T> : IRxReadOnlyList<T>
        {
            private readonly IRxList<T> wrapped;

            public RxReadOnlyListWrapper(IRxList<T> wrapped)
            {
                this.wrapped = wrapped;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this.wrapped.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.wrapped.GetEnumerator();
            }

            public T this[int index]
            {
                get { return this.wrapped[index]; }
            }

            public IObservable<NotifyCollectionChangedEventArgs> Changed
            {
                get { return this.wrapped.Changed;}
            }

            public int Count
            {
                get { return this.wrapped.Count; }
            }
        }
    }
}

