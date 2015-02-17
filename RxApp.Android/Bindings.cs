using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Android.Widget;
using Android.Support.V4.Widget;

namespace RxApp
{
    public static class Bindings
    {
        public static IDisposable BindTo(this IRxCommand This, Button button)
        {
            var subscription = new CompositeDisposable();
            subscription.Add(
                This.CanExecute.Subscribe(x => button.Enabled = x));
            subscription.Add(
                Observable.FromEventPattern(button, "Click").InvokeCommand(This));
            return subscription;
        }

        public static IDisposable BindTo(this IRxCommand This, SwipeRefreshLayout refresher)
        {
            var subscription = new CompositeDisposable();
            subscription.Add(
                This.CanExecute.Subscribe(x => refresher.Refreshing = x));
            subscription.Add(
                Observable.FromEventPattern(refresher, "Refresh").InvokeCommand(This));
            return subscription;
        }

        public static IDisposable BindTo(this IObservable<string> This, TextView textView)
        {
            return This.Subscribe(x => textView.Text = x);
        }

        public static IDisposable BindTo(this IRxProperty<bool> This, CompoundButton button)
        {
            var subscription = new CompositeDisposable();
            subscription.Add(
                Observable.FromEventPattern<CompoundButton.CheckedChangeEventArgs>(button, "CheckedChange").Subscribe(x =>
                    {
                        This.Value = x.EventArgs.IsChecked;
                    }));
            subscription.Add(This.Subscribe(x => { if (button.Checked != x) { button.Checked = x; } }));
            return subscription;
        }
    }
}

