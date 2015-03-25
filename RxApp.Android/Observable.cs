using System;
using System.Reactive.Linq;

namespace RxApp.Android
{
    public static partial class Observable
    {
        public static IObservable<T> ObserveOnMainThread<T>(this IObservable<T> This)
        {
            return This.ObserveOn(Scheduler.MainThreadScheduler);
        }
    }
}