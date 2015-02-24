using System;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp
{
    public static partial class Observable
    {
        internal static IDisposable BindTo<T, TView>(this IObservable<T> This, TView target, Expression<Func<TView, T>> property, IScheduler scheduler)
        {
            var propertySetter = Reflection.GetSetter(property);

            return This.ObserveOn(scheduler)
                       .Subscribe(x => propertySetter(target, x));
        }

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
            return RxObservable.CombineLatest(_1, _2, _3, (_r1, _r2, _r3) => Tuple.Create(_r1, _r2, _r3));
        }

        public static IObservable<Tuple<T1,T2,T3,T4>> CombineLatest<T1,T2,T3,T4>(
            this IObservable<T1> _1, 
            IObservable<T2> _2,
            IObservable<T3> _3,
            IObservable<T4> _4)
        {
            return RxObservable.CombineLatest(_1, _2, _3, _4,  (_r1, _r2, _r3, _r4) => Tuple.Create(_r1, _r2, _r3, _r4));
        }

        public static IObservable<Tuple<T1,T2,T3,T4,T5>> CombineLatest<T1,T2,T3,T4,T5>(
            this IObservable<T1> _1, 
            IObservable<T2> _2,
            IObservable<T3> _3,
            IObservable<T4> _4,
            IObservable<T5> _5)
        {
            return RxObservable.CombineLatest(_1, _2, _3, _4, _5, (_r1, _r2, _r3, _r4, _r5) => Tuple.Create(_r1, _r2, _r3, _r4, _r5));
        }

        public static IObservable<Tuple<T1,T2,T3,T4,T5,T6>> CombineLatest<T1,T2,T3,T4,T5,T6>(
            this IObservable<T1> _1, 
            IObservable<T2> _2,
            IObservable<T3> _3,
            IObservable<T4> _4,
            IObservable<T5> _5,
            IObservable<T6> _6)
        {
            return RxObservable.CombineLatest(_1, _2, _3, _4, _5, _6, (_r1, _r2, _r3, _r4, _r5, _r6) => 
                Tuple.Create(_r1, _r2, _r3, _r4, _r5, _r6));
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
            return RxObservable.CombineLatest(_1, _2, _3, _4, _5, _6, _7, (_r1, _r2, _r3, _r4, _r5, _r6, _r7) => 
                Tuple.Create(_r1, _r2, _r3, _r4, _r5, _r6, _r7));
        }

        public static IObservable<Tuple<T1,T2>> Zip<T1, T2>(
            this IObservable<T1> _1, 
            IObservable<T2> _2)
        {
            return RxObservable.Zip(_1, _2, (_r1, _r2) => Tuple.Create(_r1, _r2));
        }
    }

    public static class Disposable
    {
        public static IDisposable Compose(IDisposable first, IDisposable second)
        {
            return RxDisposable.Create(() => 
                {
                    first.Dispose();
                    second.Dispose();
                });
        }

        public static IDisposable Compose(IDisposable first, IDisposable second, params IDisposable[] disposables)
        {
            return RxDisposable.Create(() => 
                {
                    first.Dispose();
                    second.Dispose();
                    foreach (var disposable in disposables) { disposable.Dispose(); }
                });
        }
    }
}

