using System;
using System.Collections.Generic;
using System.Linq;

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Foundation;
using UIKit;

using RxObservable = System.Reactive.Linq.Observable;
using System.Reflection;

namespace RxApp.iOS
{
    public sealed class RxUIApplicationDelegateHelper
    {
        public static RxUIApplicationDelegateHelper Create(
            Func<INavigationApp> getNavigationApp,
            Func<INavigationViewModel, UIViewController> provideView)
        {
            return new RxUIApplicationDelegateHelper(getNavigationApp, provideView);
        }

        private readonly Func<INavigationApp> getNavigationApp;
        private readonly Func<INavigationViewModel, UIViewController> provideView;

        private IDisposable subscription;

        private RxUIApplicationDelegateHelper(
            Func<INavigationApp> getNavigationApp,
            Func<INavigationViewModel, UIViewController> provideView)
        {
            this.getNavigationApp = getNavigationApp;
            this.provideView = provideView;
        }

        public bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var navStack = NavigationStack<INavigationModel>.Create(Scheduler.MainThreadScheduler);
            var navViewController = new BufferedNavigationController(navStack);
            var navigationController = getNavigationApp();
            var views = new Dictionary<INavigationViewModel, UIViewController>();

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
                        navViewController.SetViewControllers(viewControllers, true);

                        foreach (var model in removed)
                        {
                            IDisposable view = views[model];
                            views.Remove(model);
                            view.Dispose();
                        }
                    }),

                navStack.BindTo(x => navigationController.Bind(x)),
                navigationController.RootState.BindTo(navStack.SetRoot)
            );

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
        private readonly NavigationStack<INavigationModel> navStack;

        private bool transitioning = false;

        public BufferedNavigationController(NavigationStack<INavigationModel> navStack): base()
        {
            this.navStack = navStack;
            this.WeakDelegate = this;
        }
            
        public override UIViewController PopViewController (bool animated)
        {
            this.actions.Enqueue(() => 
                {
                    // FIXME: This cast is pretty ugly. Works in practice, but fragile.
                    this.navStack.Select(x => ((INavigationViewModel) x).Back).First().Execute();
                });
            return base.PopViewController(animated);
        }

        public override UIViewController[] PopToRootViewController(bool animated)
        {
            this.actions.Enqueue(() => 
                {
                    // FIXME: This cast is pretty ugly. Works in practice, but fragile.
                    this.navStack.Select(x => ((INavigationViewModel) x).Up).First().Execute();
                });
            return base.PopToRootViewController(animated);
        }

        public override UIViewController[] PopToViewController(UIViewController viewController, bool animated)
        {
            return null;
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

        public override void PushViewController(UIViewController viewController, bool animated)
        {
            throw new NotSupportedException();
        }

        [Export("navigationController:didShowViewController:animated:")]
        public void DidShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
        {
            this.transitioning = false;
            runNextAction();
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

        private void runNextAction()
        {
            if (actions.Count > 0)
            {
                var action = actions.Dequeue();
                action();
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
                    this.GetNavigationApp,
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

        protected abstract INavigationApp GetNavigationApp();

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

