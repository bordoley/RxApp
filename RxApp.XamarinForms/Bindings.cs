using System;
using System.Linq.Expressions;
using System.Reactive;
using Xamarin.Forms;

using RxObservable = System.Reactive.Linq.Observable;
using System.Reactive.Linq;

namespace RxApp.XamarinForms
{
    public static class Bindings
    {
        public static IDisposable BindTo<T, TView>(this IObservable<T> This, TView target, Expression<Func<TView, T>> property)
        {
            return This.BindTo(target, property, Scheduler.MainThreadScheduler);
        }

        public static IDisposable BindTo(this IObservable<Unit> This, Action action)
        {
            return This.ObserveOnMainThread().Subscribe(_ => action());
        }

        public static IDisposable BindTo<T>(this IObservable<T> This, Action<T> action)
        {
            return This.ObserveOnMainThread().Subscribe(action);
        }

        public static IDisposable Bind(this IRxCommand This, Button button)
        {
            return Disposable.Compose(
                This.CanExecute.ObserveOnMainThread().Subscribe(x => button.IsEnabled = x),
                RxObservable.FromEventPattern(button, "Clicked").InvokeCommand(This)
            );
        }
    }
}

