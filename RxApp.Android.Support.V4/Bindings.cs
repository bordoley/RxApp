using System;

using Android.Support.V4.Widget;

using RxObservable = System.Reactive.Linq.Observable;
using System.Reactive.Linq;
using System.Reactive;

namespace RxApp.Android
{
    public static partial class Bindings
    {
        public static IDisposable Bind(this IRxCommand This, SwipeRefreshLayout refresher)
        {   
            return Disposable.Compose(
                RxObservable.FromEventPattern(refresher, "Refresh").Select(_ => Unit.Default)
                            .Subscribe(x => This.Execute()),
                This.CanExecute.ObserveOnMainThread().Subscribe(x => refresher.Enabled = x)
            );
        }
    }
}

