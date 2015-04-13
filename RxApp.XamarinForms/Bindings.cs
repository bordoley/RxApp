using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using Xamarin.Forms;

using RxObservable = System.Reactive.Linq.Observable;
using System.Reactive.Linq;

namespace RxApp.XamarinForms
{
    public static class Bindings
    {
        public static IDisposable BindTo(
            this IObservable<NavigationStack> This,
            RxFormsApplication application, 
            Func<INavigationViewModel,Page> createPage)
        {
            var navigationPage = application.MainPage;
            var popping = false;

            var stack = This
                .ObserveOnMainThread()
                .Scan(Tuple.Create(RxApp.NavigationStack.Empty, RxApp.NavigationStack.Empty), (acc, navStack) => Tuple.Create(acc.Item2, navStack))
                .SelectMany(async x =>
                    {
                        var previousNavStack = x.Item1;
                        var currentNavStack = x.Item2;

                        var currentPage = (navigationPage.CurrentPage as IReadOnlyViewFor);
                        var navPageModel = (currentPage != null) ? (currentPage.ViewModel as INavigationViewModel) : null;

                        var head = currentNavStack.FirstOrDefault();

                        if (currentNavStack.IsEmpty)
                        {
                            // Do nothing. Can only happen on Android. Android handles the stack being empty by
                            // killing the activity.
                        }

                        else if (head == navPageModel)
                        {
                            // Do nothing, means the user clicked the back button which we cant intercept,
                            // so we let forms pop the view, listen for the popping event, and then popped the view model.
                        }

                        else if (currentNavStack.Pop().Equals(previousNavStack))
                        {
                            var view = createPage(head);
                            await navigationPage.PushAsync(view, true);
                        }

                        else if (previousNavStack.Pop().Equals(currentNavStack))
                        {
                            // Programmatic back button was clicked
                            popping = true;
                            await navigationPage.PopAsync(true);
                            popping = false;
                        }

                        else if (previousNavStack.Up().Equals(currentNavStack))
                        {
                            // Programmatic up button was clicked
                            popping = true;
                            await navigationPage.PopToRootAsync(true);
                            popping = false;
                        }

                        return currentNavStack;
                    })
                .Publish();

            return Disposable.Compose(
                stack.Where(x => x.IsEmpty).Subscribe(_ => application.SendDone()),

                // Handle the user clicking the back button
                RxObservable.FromEventPattern<NavigationEventArgs>(navigationPage, "Popped")
                    .Where(_ => !popping)
                    .Subscribe(e =>
                        {
                            var vm = ((e.EventArgs.Page as IReadOnlyViewFor).ViewModel as INavigationViewModel);
                            vm.Activate.Execute();
                            vm.Back.Execute();
                            vm.Deactivate.Execute();
                        }),
                stack.Connect()
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

        public static IDisposable Bind(this IRxCommand This, Button button)
        {
            return Disposable.Compose(
                This.CanExecute.ObserveOnMainThread().Subscribe(x => button.IsEnabled = x),
                RxObservable.FromEventPattern(button, "Clicked").InvokeCommand(This)
            );
        }
    }
}

