using System;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp
{
    public static partial class Observable
    {
        public static IObservable<Tuple<T1,T2>> CombineLatest<T1,T2>(
            IObservable<T1> source1, 
            IObservable<T2> source2)
        {
            return RxObservable.CombineLatest(
                source1, source2, 
                (result1, result2) => Tuple.Create(result1, result2));
        }

        public static IObservable<Tuple<T1,T2,T3>> CombineLatest<T1,T2,T3>(
            IObservable<T1> source1, 
            IObservable<T2> source2,
            IObservable<T3> source3)
        {
            return RxObservable.CombineLatest(
                source1, source2, source3, 
                (result1, result2, result3) => 
                    Tuple.Create(result1, result2, result3));
        }

        public static IObservable<Tuple<T1,T2,T3,T4>> CombineLatest<T1,T2,T3,T4>(
            IObservable<T1> source1, 
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4)
        {
            return RxObservable.CombineLatest(
                source1, source2, source3, source4,  
                (result1, result2, result3, result4) => 
                    Tuple.Create(result1, result2, result3, result4));
        }

        public static IObservable<Tuple<T1,T2,T3,T4,T5>> CombineLatest<T1,T2,T3,T4,T5>(
            IObservable<T1> source1, 
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5)
        {
            return RxObservable.CombineLatest(
                source1, source2, source3, source4, source5, 
                (result1, result2, result3, result4, result5) => 
                    Tuple.Create(result1, result2, result3, result4, result5));
        }

        public static IObservable<Tuple<T1,T2,T3,T4,T5,T6>> CombineLatest<T1,T2,T3,T4,T5,T6>(
            IObservable<T1> source1, 
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5,
            IObservable<T6> source6)
        {
            return RxObservable.CombineLatest(
                source1, source2, source3, source4, source5, source6, 
                (result1, result2, result3, result4, result5, result6) => 
                    Tuple.Create(result1, result2, result3, result4, result5, result6));
        }

        public static IObservable<Tuple<T1,T2,T3,T4,T5,T6,T7>> CombineLatest<T1,T2,T3,T4,T5,T6,T7>(
            IObservable<T1> source1, 
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5,
            IObservable<T6> source6,
            IObservable<T7> source7)
        {
            return RxObservable.CombineLatest(
                source1, source2, source3, source4, source5, source6, source7,
                (result1, result2, result3, result4, result5, result6, result7) => 
                    Tuple.Create(result1, result2, result3, result4, result5, result6, result7));
        }
    }
}

