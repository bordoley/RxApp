using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using Xamarin.Forms;

using RxObservable = System.Reactive.Linq.Observable;
using System.Reflection;

namespace RxApp.XamarinForms
{
    public sealed class RxFormsApplicationBuilder
    {
        private static Page CreatePage(
            IReadOnlyDictionary<Type, Func<INavigationViewModel,Page>> modelToPageCreator,
            INavigationViewModel model)
        {
            var modelType = model.GetType();

            foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
            {
                Func<INavigationViewModel,Page> pageCreator;
                if (modelToPageCreator.TryGetValue(iface, out pageCreator))
                {
                    return pageCreator(model);
                }
            }

            throw new NotSupportedException("No Page creator found for the given model type: " + modelType);
        }

        private readonly Dictionary<Type, Func<INavigationViewModel,Page>> modelToPageCreator = 
            new Dictionary<Type, Func<INavigationViewModel,Page>>();

        private IObservable<NavigationStack> navigationStack;
        private NavigationPage navigationPage;
        private RxFormsApplication application;

        public IObservable<NavigationStack> NavigationStack
        { 
            set { this.navigationStack = value; }
        }

        public NavigationPage NavigationPage
        { 
            set { this.navigationPage = value; }
        }

        public RxFormsApplication Application
        { 
            set { this.application = value; }
        }

        public void RegisterPageCreator<TModel, TView>(Func<TModel,TView> viewCreator)
            where TModel : INavigationViewModel
            where TView : Page
        {
            this.modelToPageCreator.Add(
                typeof(TModel), 
                model => viewCreator((TModel) model));
        }

        public IDisposable Build()
        {
            var modelToPageCreator = this.modelToPageCreator.ToImmutableDictionary();

            if (this.navigationStack == null) { throw new NotSupportedException("NavigationStack must not be null"); }
            var navigationStack = this.navigationStack;

            var navigationPage = this.navigationPage ?? new NavigationPage();

            if (this.application == null) { throw new NotSupportedException("Application must not be null"); }
            var formsApplication = this.application;

            formsApplication.MainPage = navigationPage;

            var popping = false;

            var stack = navigationStack
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
                            var view = CreatePage(modelToPageCreator, head);
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
                stack.Where(x => x.IsEmpty).Subscribe(_ => formsApplication.SendDone()),

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
    }

    public class RxFormsApplication : Application
    {
        public RxFormsApplication()
        {

        }

        public event EventHandler Done = (o,e) => {};

        public event EventHandler Started = (o,e) => {};

        public event EventHandler Resumed = (o,e) => {};

        public event EventHandler Sleeping = (o,e) => {};

        protected override sealed void OnResume()
        {
            Resumed(this, null);
        }

        protected override sealed void OnSleep()
        {
            Sleeping(this, null);
        }

        protected override sealed void OnStart()
        {
            Started(this, null);
        }

        internal void SendDone()
        {
            Done(this, null);
        }
    }
}

