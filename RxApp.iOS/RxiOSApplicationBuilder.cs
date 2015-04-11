using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Foundation;
using UIKit;

using RxObservable = System.Reactive.Linq.Observable;
using System.Reflection;
using System.Reactive.Subjects;

namespace RxApp.iOS
{
    public sealed class RxiOSApplicationBuilder
    {
        private static UIViewController CreateViewController (
            IReadOnlyDictionary<Type, Func<INavigationViewModel,UIViewController>> modelToViewCreator,
            INavigationViewModel model)
        {
            var modelType = model.GetType();

            foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
            {
                Func<INavigationViewModel,UIViewController> viewCreator;
                if (modelToViewCreator.TryGetValue(iface, out viewCreator))
                {
                    return viewCreator(model);
                }
            }

            throw new NotSupportedException("No UIViewController found for the given model type: " + modelType);
        }

        private readonly Dictionary<Type, Func<INavigationViewModel,UIViewController>> modelToViewCreator =
            new Dictionary<Type, Func<INavigationViewModel,UIViewController>>();

        private IObservable<NavigationStack> navigationStack;
        private UIWindow window;
        private RxUINavigationController uiNavigationController;

        public void RegisterViewCreator<TModel, TView>(Func<TModel,TView> viewCreator)
            where TModel : INavigationViewModel
            where TView : UIViewController
        {
            this.modelToViewCreator.Add(
                typeof(TModel), 
                model => viewCreator((TModel) model));
        }

        public IObservable<NavigationStack> NavigationStack
        { 
            set { this.navigationStack = value; }
        }

        public UIWindow Window
        { 
            set { this.window = value; }
        }

        public RxUINavigationController UINavigationController
        {
            set { this.uiNavigationController = value; }
        }

        public IDisposable Build()
        {
            var modelToViewCreator = this.modelToViewCreator.ToImmutableDictionary();

            if (this.navigationStack == null) { throw new NotSupportedException("Application must not be null"); }
            var navigationStack = this.navigationStack;

            var navigationController = this.uiNavigationController ?? new RxUINavigationController();
            var window = this.window ?? new UIWindow(UIScreen.MainScreen.Bounds);

            window.RootViewController = navigationController;
            window.MakeKeyAndVisible();

            var viewControllers = new Dictionary<INavigationViewModel, UIViewController>();

            return navigationStack
                .ObserveOnMainThread()
                .Do(navStack =>
                    {
                        var head = navStack.FirstOrDefault();
                        var removed = viewControllers.Keys.Where(x => !navStack.Contains(x)).ToImmutableArray();

                        if (!viewControllers.ContainsKey(head))
                        {
                            var view = CreateViewController(modelToViewCreator, head);
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
    }
}

