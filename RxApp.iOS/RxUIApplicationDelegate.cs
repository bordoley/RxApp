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
    public sealed class RxUIApplicationDelegateHelper
    {
        public static RxUIApplicationDelegateHelper Create(
            Func<IObservable<NavigationStack>> getApplication,
            Func<INavigationViewModel, UIViewController> provideView)
        {
            return new RxUIApplicationDelegateHelper(getApplication, provideView);
        }

        private readonly Func<IObservable<NavigationStack>> getApplication;
        private readonly Func<INavigationViewModel, UIViewController> provideView;

        private IDisposable subscription;

        private RxUIApplicationDelegateHelper(
            Func<IObservable<NavigationStack>> getApplication,
            Func<INavigationViewModel, UIViewController> provideView)
        {
            this.getApplication = getApplication;
            this.provideView = provideView;
        }

        public bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var navViewController = new BufferedNavigationController();

            subscription = getApplication()
                .ObserveOnMainThread()
                .Scan(ImmutableDictionary<INavigationViewModel, UIViewController>.Empty, (acc, navStack) =>
                    {
                        var head = navStack.FirstOrDefault();
                        var removed = acc.Keys.Where(y => !navStack.Contains(y)).ToImmutableArray();

                        if (!acc.ContainsKey(head))
                        {
                            var view = provideView(head);
                            acc = acc.Add(head, view);
                        }

                        foreach (var model in removed)
                        {
                            IDisposable view = acc[model];
                            acc = acc.Remove(model);
                            view.Dispose();
                        }

                        var viewControllers = navStack.Reverse().Select(model => acc[model]).ToArray();
                        navViewController.SetViewControllers(viewControllers, true);

                        return acc;
                    }).Subscribe();

            var window = new UIWindow(UIScreen.MainScreen.Bounds);
            window.RootViewController = navViewController;
            window.MakeKeyAndVisible();

            return true;
        }

        public void WillTerminate(UIApplication app)
        {
            subscription.Dispose();
        }
    }

    // See: https://github.com/Plasma/BufferedNavigationController/blob/master/BufferedNavigationController.m
    internal class BufferedNavigationController : UINavigationController
    {
        private readonly Queue<Action> actions = new Queue<Action>();

        private bool transitioning = false;

        public BufferedNavigationController(): base()
        {
            this.WeakDelegate = this;
        }
      
        public override UIViewController PopViewController(bool animated)
        {
            var viewFor = (IViewFor)this.TopViewController;
            var viewModel = (INavigationViewModel)viewFor.ViewModel;

            if (this.transitioning)
            {
                this.actions.Enqueue(() => this.PopViewController(animated));
                return null;
            }
            else
            {
                var result = base.PopViewController(animated);
                viewModel.Back.Execute();
                return result;
            }
        }

        public override void SetViewControllers(UIViewController[] controllers, bool animated)
        {
            if (this.transitioning)
            {
                this.actions.Enqueue(() => this.SetViewControllers(controllers, animated));
            }
            else
            {
                base.SetViewControllers(controllers, animated);
            }
        }

        public override UIViewController[] PopToRootViewController(bool animated)
        {
            throw new NotSupportedException();
        }

        public override UIViewController[] PopToViewController(UIViewController viewController, bool animated)
        {
            return null;
        }

        public override void PushViewController(UIViewController viewController, bool animated)
        {
            throw new NotSupportedException();
        }

        [Export("navigationController:didShowViewController:animated:")]
        public void DidShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
        {
            this.transitioning = false;
            if (actions.Count > 0) { actions.Dequeue()(); }
        }

        [Export("navigationController:willShowViewController:animated:")]
        public void WillShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
        {
            this.transitioning = true;

            var transitionCoordinator = this.TopViewController.GetTransitionCoordinator();
            if (transitionCoordinator != null)
            {
                transitionCoordinator.NotifyWhenInteractionEndsUsingBlock(ctx =>
                    {
                        if (ctx.IsCancelled)
                        {
                            this.transitioning = false;
                        }
                    });
            }
        }
    }

    public abstract class RxUIApplicationDelegate : UIApplicationDelegate
    {
        private readonly RxUIApplicationDelegateHelper helper;
        private readonly Dictionary<Type, Func<INavigationViewModel,UIViewController>> modelToViewController =
            new Dictionary<Type, Func<INavigationViewModel,UIViewController>>();

        public RxUIApplicationDelegate()
        {
            helper = 
                RxUIApplicationDelegateHelper.Create(
                    this.BuildNavigationApplication,
                    this.GetUIViewController);
        }

        protected void RegisterViewCreator<TModel, TView>(Func<TModel,TView> viewCreator)
            where TModel : INavigationViewModel
            where TView : UIViewController
        {
            this.modelToViewController.Add(
                typeof(TModel), 
                model => viewCreator((TModel) model));
        }

        protected abstract IObservable<NavigationStack> BuildNavigationApplication();

        private UIViewController GetUIViewController(INavigationViewModel model)
        {
            var modelType = model.GetType();

            foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
            {
                Func<INavigationViewModel,UIViewController> viewCreator;
                if (this.modelToViewController.TryGetValue(iface, out viewCreator))
                {
                    return viewCreator(model);
                }
            }

            throw new NotSupportedException("No UIViewController found for the given model type: " + modelType);
        }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            return helper.FinishedLaunching(app, options);
        }

        public override void WillTerminate(UIApplication app)
        {
            helper.WillTerminate(app);
        }
    }
}

