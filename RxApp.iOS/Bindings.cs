using System;
using System.Linq.Expressions;

using Foundation;
using UIKit;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp.iOS
{
    public static class Bindings
    {
        public static IDisposable Bind(this IRxProperty<bool> This, UISwitch uiSwitch)
        {   return Disposable.Compose(
                RxObservable.FromEventPattern(uiSwitch, "ValueChanged")
                            .Subscribe(x => { This.Value = uiSwitch.On; }),
                This.ObserveOnMainThread()
                    .Subscribe(x => { if (uiSwitch.On != x) { uiSwitch.On = x; } })
            );
        }

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

