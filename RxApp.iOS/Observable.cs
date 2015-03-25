using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using CoreFoundation;
using Foundation;
using System.Reactive.Disposables;

namespace RxApp.iOS
{
    public static class Observable
    {
        public static IObservable<T> ObserveOnMainThread<T>(this IObservable<T> observable)
        {
            return observable.ObserveOn(Scheduler.MainThreadScheduler);
        }
    }
}

