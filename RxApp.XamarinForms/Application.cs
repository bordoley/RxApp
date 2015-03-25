using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly NavigationPage navigationPage = new NavigationPage();
        private readonly NavigationStack<INavigationModel> navStack = 
            NavigationStack<INavigationModel>.Create(Scheduler.MainThreadScheduler);

        private readonly IObservable<INavigationModel> rootState;
        private readonly Func<INavigationControllerModel, IDisposable> bindController;
        private readonly Func<INavigationViewModel, Page> provideView;

        private IDisposable subscription;

        private RxApplicationHelper(
            IObservable<INavigationModel> rootState,
            Func<INavigationControllerModel, IDisposable> bindController,
            Func<INavigationViewModel, Page> provideView)
        {
            this.rootState = rootState;
            this.bindController = bindController;
            this.provideView = provideView;
        }

        public Page MainPage { get { return navigationPage; } }

        public void OnStart()
        {
            var navStack = NavigationStack<INavigationModel>.Create(Scheduler.MainThreadScheduler);

            subscription = Disposable.Compose(
                RxObservable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs<INavigationModel>>(navStack, "NavigationStackChanged")
                    .Subscribe(async (EventPattern<NotifyNavigationStackChangedEventArgs<INavigationModel>> e) =>
                    {
                        var newHead = e.EventArgs.NewHead;
                        var removed = e.EventArgs.Removed.Count();

                        if (removed == 0)
                        {
                            var view = provideView(newHead);
                            await this.navigationPage.PushAsync(view, true);
                        } 
                        else if (removed == 1)
                        {
                            await this.navigationPage.PopAsync(true);
                        }
                        else
                        {
                            await this.navigationPage.PopToRootAsync(true);
                        }
                    }),

                navStack.BindTo(x => this.bindController(x)),
                rootState.ObserveOnMainThread().Subscribe(navStack.SetRoot)
            );
        }

    }
}

