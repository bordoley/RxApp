using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp
{
    public static partial class Observable
    {
        public static IObservable<Tuple<T1,T2>> CombineLatest<T1,T2>(
            this IObservable<T1> _1, 
            IObservable<T2> _2)
        {
            return RxObservable.CombineLatest(_1, _2, (_r1, _r2) => Tuple.Create(_r1, _r2));
        }

        public static IObservable<Tuple<T1,T2,T3>> CombineLatest<T1,T2,T3>(
            this IObservable<T1> _1, 
            IObservable<T2> _2,
            IObservable<T3> _3)
        {
            return Observable.CombineLatest(_1, _2).CombineLatest(_3)
                              .Select(t => Tuple.Create(t.Item1.Item1, t.Item1.Item2, t.Item2));
        }

        public static IObservable<Tuple<T1,T2,T3,T4>> CombineLatest<T1,T2,T3,T4>(
            this IObservable<T1> _1, 
            IObservable<T2> _2,
            IObservable<T3> _3,
            IObservable<T4> _4)
        {
            return Observable.CombineLatest(_1, _2).CombineLatest(_3, _4)
                              .Select(t => Tuple.Create(t.Item1.Item1, t.Item1.Item2, t.Item2, t.Item3));
        }

        public static IObservable<Tuple<T1,T2,T3,T4,T5>> CombineLatest<T1,T2,T3,T4,T5>(
            this IObservable<T1> _1, 
            IObservable<T2> _2,
            IObservable<T3> _3,
            IObservable<T4> _4,
            IObservable<T5> _5)
        {
            return Observable.CombineLatest(_1, _2, _3).CombineLatest(_4, _5)
                              .Select(t => Tuple.Create(t.Item1.Item1, t.Item1.Item2, t.Item1.Item3, t.Item2, t.Item3));
        }

        public static IObservable<Tuple<T1,T2,T3,T4,T5,T6>> CombineLatest<T1,T2,T3,T4,T5,T6>(
            this IObservable<T1> _1, 
            IObservable<T2> _2,
            IObservable<T3> _3,
            IObservable<T4> _4,
            IObservable<T5> _5,
            IObservable<T6> _6)
        {
            return Observable.CombineLatest(_1, _2, _3).CombineLatest(_4, _5, _6)
                              .Select(t => Tuple.Create(t.Item1.Item1, t.Item1.Item2, t.Item1.Item3, t.Item2, t.Item3, t.Item4));
        }

        public static IObservable<Tuple<T1,T2,T3,T4,T5,T6,T7>> CombineLatest<T1,T2,T3,T4,T5,T6,T7>(
            this IObservable<T1> _1, 
            IObservable<T2> _2,
            IObservable<T3> _3,
            IObservable<T4> _4,
            IObservable<T5> _5,
            IObservable<T6> _6,
            IObservable<T7> _7)
        {
            return Observable.CombineLatest(_1, _2, _3).CombineLatest(_4, _5, _6, _7)
                              .Select(t => Tuple.Create(t.Item1.Item1, t.Item1.Item2, t.Item1.Item3, t.Item2, t.Item3, t.Item4, t.Item5));
        }
    }

    public static class Disposable
    {
        public static IDisposable Combine(IDisposable first, IDisposable second)
        {
            var result = new CompositeDisposable();
            result.Add(first);
            result.Add(second);
            return result;
        }

        public static IDisposable Combine(IDisposable first, IDisposable second, params IDisposable[] disposables)
        {
            var result = new CompositeDisposable();
            result.Add(first);
            result.Add(second);

            foreach (var disposable in disposables)
            {
                result.Add(disposable);
            }

            return result;
        }
    }
}

