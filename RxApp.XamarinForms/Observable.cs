using System;
using System.Reactive.Concurrency;
using System.Threading.



namespace RxApp.XamarinForms
{
    public static partial class Observable
    {   
        private static readonly IScheduler _mainThreadScheduler = 
            new DeviceScheduler();

        internal static IScheduler MainThreadScheduler { get { return _mainThreadScheduler; } } 

        internal sealed class DeviceScheduler : IScheduler
        {
            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
            {
                throw new NotImplementedException();
            }

            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                throw new NotImplementedException();
            }

            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                throw new NotImplementedException();
            }

            public DateTimeOffset Now { get { return DateTimeOffset.Now; } } }
        }
    }
}

