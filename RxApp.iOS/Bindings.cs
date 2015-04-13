using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;

using Foundation;
using UIKit;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Collections.Immutable;

namespace RxApp.iOS
{
    public static class Bindings
    {
        private static DateTime ToDateTime(this NSDate date)
        {
            DateTime reference = TimeZone.CurrentTimeZone.ToLocalTime(
                new DateTime(2001, 1, 1, 0, 0, 0) );
            return reference.AddSeconds(date.SecondsSinceReferenceDate);
        }

        private static NSDate ToNSDate(this DateTime date)
        {
            DateTime reference = TimeZone.CurrentTimeZone.ToLocalTime(
                new DateTime(2001, 1, 1, 0, 0, 0) );
            return NSDate.FromTimeIntervalSinceReferenceDate(
                (date - reference).TotalSeconds);
        }

        public static IDisposable BindTo(
            this IObservable<NavigationStack> This, 
            RxUINavigationController navigationController, 
            Func<INavigationViewModel,UIViewController> createView)
        {
            var viewControllers = new Dictionary<INavigationViewModel, UIViewController>();

            return This
                .ObserveOnMainThread()
                .Do(navStack =>
                    {
                        var head = navStack.FirstOrDefault();
                        var removed = viewControllers.Keys.Where(x => !navStack.Contains(x)).ToImmutableArray();

                        if (!viewControllers.ContainsKey(head))
                        {
                            var view = createView(head);
                            viewControllers.Add(head, view);
                        }

                        foreach (var model in removed)
                        {
                            IDisposable view = viewControllers[model];
                            viewControllers.Remove(model);
                            view.Dispose();
                        }

                        navigationController.SetViewControllers(
                            navStack.Reverse().Select(model => viewControllers[model]).ToArray(), 
                            true);
                    }).Subscribe();
        }

        public static IDisposable Bind(this IRxProperty<bool> This, UISwitch uiSwitch)
        {   
            return Disposable.Compose(
                RxObservable.FromEventPattern(uiSwitch, "ValueChanged")
                            .Subscribe(x => { This.Value = uiSwitch.On; }),
                This.BindTo(x => { if (uiSwitch.On != x) { uiSwitch.On = x; } })
            );
        }

        public static IDisposable Bind(this IRxProperty<DateTime> This, UIDatePicker datePicker)
        {
            return Disposable.Compose(
                RxObservable.FromEventPattern(datePicker, "ValueChanged")
                            .Subscribe(x => { This.Value = datePicker.Date.ToDateTime(); }),

                This.BindTo(x => 
                    { 
                        var datePickerDate = datePicker.Date.ToDateTime();
                        if (datePickerDate != x) 
                        { 
                            datePicker.Date = x.ToNSDate(); 
                        } 
                    })
            );
        }

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

        public static IDisposable Bind(this IRxCommand This, UIButton button)
        {  
            return Disposable.Compose(
                This.CanExecute.ObserveOnMainThread().Subscribe(x => button.Enabled = x),
                RxObservable.FromEventPattern(button, "TouchUpInside").InvokeCommand(This)
            );
        }

        public static IDisposable Bind(this IRxCommand This, UIBarButtonItem button)
        {  
            return Disposable.Compose(
                This.CanExecute.ObserveOnMainThread().Subscribe(x => button.Enabled = x),
                RxObservable.FromEventPattern(button, "Clicked").InvokeCommand(This)
            );
        }
    }
}

