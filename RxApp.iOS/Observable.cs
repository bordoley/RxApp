using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using CoreFoundation;
using Foundation;
using System.Reactive.Disposables;

using RxDisposable = System.Reactive.Disposables.Disposable;

namespace RxApp.iOS
{
    public static class Observable
    {
        private static readonly IScheduler _mainThreadScheduler = 
            new NSRunLoopScheduler();

        internal static IScheduler MainThreadScheduler { get { return _mainThreadScheduler; } } 

        public static IObservable<T> ObserveOnMainThread<T>(this IObservable<T> observable)
        {
            return observable.ObserveOn(_mainThreadScheduler);
        }

        private sealed class NSRunLoopScheduler : IScheduler
        {
            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
            {
                var innerDisp = new SingleAssignmentDisposable();

                DispatchQueue.MainQueue.DispatchAsync(() => 
                    {
                        if (!innerDisp.IsDisposed) innerDisp.Disposable = action(this, state);
                    });
                
                return innerDisp;
            }

            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                var innerDisp = RxDisposable.Empty;
                bool isCancelled = false;
                        
                var timer = NSTimer.CreateScheduledTimer(dueTime, _ => 
                    {
                        if (!isCancelled) innerDisp = action(this, state);
                    });
            
                return RxDisposable.Create(() => 
                    {
                        isCancelled = true;
                        timer.Invalidate();
                        innerDisp.Dispose();
                    });
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

