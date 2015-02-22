using System;
using System.Linq.Expressions;
using UIKit;

using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp.iOS
{
    public static class Bindings
    {
        public static IDisposable BindTo<T, TView>(this IObservable<T> This, TView target, Expression<Func<TView, T>> property)
        {
            return This.BindTo(target, property, Observable.MainThreadScheduler);
        }

        public static IDisposable Bind(this IRxCommand This, UIButton button)
        {
            return Disposable.Compose(
                This.CanExecute.ObserveOnMainThread().Subscribe(x => button.Enabled = x),
                RxObservable.FromEventPattern(button, "TouchUpInside").InvokeCommand(This)
            );
        }
    }
}

