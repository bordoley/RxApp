using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

using Xamarin.Forms;

using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp.XamarinForms
{
    public sealed class RxApplicationHelper
    {
        public static RxApplicationHelper Create(
            IObservable<INavigationModel> rootState,
            Func<INavigationControllerModel, IDisposable> bindController,
            Func<INavigationViewModel, Page> provideView)
        {
            return new RxApplicationHelper(rootState, bindController, provideView);
        }

        private readonly Page mainPage = new NavigationPage();
        private readonly NavigationStack<INavigationModel> navStack = NavigationStack<INavigationModel>.Create(Observable.MainThreadScheduler);

        private readonly IObservable<INavigationModel> rootState;
        private readonly Func<INavigationControllerModel, IDisposable> bindController;
        private readonly Func<INavigationViewModel, Page> provideView;

        private IDisposable subscription;

        private RxApplicationHelper(
            IObservable<INavigationModel> rootState,
            Func<INavigationControllerModel, IDisposable> bindController,
            Func<INavigationViewModel, Page> provideView)
        {
        }

        public Page MainPage { get { return mainPage; } }

        public void OnStart()
        {
            var navStack = NavigationStack<INavigationModel>.Create(Observable.MainThreadScheduler);
            var views = new Dictionary<INavigationViewModel, Page>();

            subscription = Disposable.Compose(
                RxObservable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs<INavigationModel>>(navStack, "NavigationStackChanged")
                    .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs<INavigationModel>> e) =>
                    {
                        var newHead = e.EventArgs.NewHead;
                        var removed = e.EventArgs.Removed;

                        if (!views.ContainsKey(newHead))
                        {
                            var view = provideView(newHead);
                            views[newHead] = view;
                        }

                        var viewControllers = navStack.Reverse().Select(x => views[x]).ToArray();
                        navController.SetViewControllers(viewControllers, true);

                        foreach (var model in removed)
                        {
                            IDisposable view = views[model];
                            views.Remove(model);
                            view.Dispose();
                        }
                    }),

                navStack.BindTo(x => this.bindController(x)),
                rootState.ObserveOnMainThread().Subscribe(navStack.SetRoot)
            );
        }

    }
}

