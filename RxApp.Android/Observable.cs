using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.OS;

using RxDisposable = System.Reactive.Disposables.Disposable;

namespace RxApp.Android
{
    public static partial class Observable
    {
        private static readonly IScheduler _mainThreadScheduler = 
            new LooperScheduler(Looper.MainLooper);

        internal static IScheduler MainThreadScheduler { get { return _mainThreadScheduler; } } 

        public static IObservable<T> ObserveOnMainThread<T>(this IObservable<T> observable)
        {
            return observable.ObserveOn(_mainThreadScheduler);
        }

        private sealed class LooperScheduler : IScheduler
        {
            Handler handler;
            long threadId;

            internal LooperScheduler(Looper looper)
            {
                this.handler = new Handler(looper);
                threadId = looper.Thread.Id;
            }

            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
            {
                bool isCancelled = false;
                var innerDisp = new SerialDisposable() { Disposable = RxDisposable.Empty };

                if (threadId == Java.Lang.Thread.CurrentThread().Id) 
                {
                    return action(this, state);
                }

                handler.Post(() => 
                    {
                        if (isCancelled) return;
                        innerDisp.Disposable = action(this, state);
                    });

                return new CompositeDisposable(
                    RxDisposable.Create(() => isCancelled = true),
                    innerDisp);
            }

            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                bool isCancelled = false;
                var innerDisp = new SerialDisposable() { Disposable = RxDisposable.Empty };

                handler.PostDelayed(() => 
                    {
                        if (isCancelled) return;
                        innerDisp.Disposable = action(this, state);
                    }, dueTime.Ticks / 10 / 1000);

                return new CompositeDisposable(
                    RxDisposable.Create(() => isCancelled = true),
                    innerDisp);
            }

            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                if (dueTime <= Now) { return Schedule(state, action); }

                return Schedule(state, dueTime - Now, action);
            }

            public DateTimeOffset Now { get { return DateTimeOffset.Now; } }
        }
    }
}  