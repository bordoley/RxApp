using System;
using System.Reactive.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;

namespace RxApp
{
    internal static class Bindings
    {
        internal static IDisposable BindTo<T, TView>(this IObservable<T> This, TView target, Expression<Func<TView, T>> property, IScheduler scheduler)
        {
            var propertySetter = property.GetSetter();

            return This.ObserveOn(scheduler)
                       .Subscribe(x => propertySetter(target, x));
        }
    }
}

