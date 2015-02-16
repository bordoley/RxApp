using System;
using System.Collections.Generic;
using System.Linq;

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Foundation;
using UIKit;

namespace RxApp
{
    public sealed class RxUIApplicationDelegateHelper
    {
        public static RxUIApplicationDelegateHelper Create(
            Func<INavigationStack, IApplication> applicationProvider,
            Func<object, UIViewController> provideView)
        {
            return new RxUIApplicationDelegateHelper(applicationProvider, provideView);
        }

        private readonly INavigationStack navStack = NavigationStack.Create();
        private readonly Func<INavigationStack, IApplication> applicationProvider;
        private readonly Func<object, UIViewController> provideView;

        private CompositeDisposable subscription = null;
        private UIWindow window;
        private IApplication application;

        private RxUIApplicationDelegateHelper(
            Func<INavigationStack, IApplication> applicationProvider,
            Func<object, UIViewController> provideView)
        {
            this.applicationProvider = applicationProvider;
            this.provideView = provideView;
        }

        public bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var navController = new BufferedNavigationController(navStack);
            var  views = new Dictionary<object, UIViewController>();

            subscription = new CompositeDisposable();
            subscription.Add(
                Observable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs>(navStack, "NavigationStackChanged")
                    .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs> e) =>
                    {
                        var newHead = e.EventArgs.NewHead;
                        var oldHead = e.EventArgs.OldHead;
                        var removed = e.EventArgs.Removed;

                        if (oldHead != null && newHead == null)
                        {
                            // On iOS this case can't really happen
                            application.Dispose();
                        }
                        else if (newHead != null && !views.ContainsKey(newHead))
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
                    }));

            subscription.Add(navStack.BindController(model => application.Bind(model)));

            window = new UIWindow(UIScreen.MainScreen.Bounds);
            window.RootViewController = navController;
            window.MakeKeyAndVisible();

            application = applicationProvider(navStack);
            application.Init();
            return true;
        }

        public void WillTerminate(UIApplication app)
        {
            subscription.Dispose();
            //application.Dispose();
        }
    }

    // See: https://github.com/Plasma/BufferedNavigationController/blob/master/BufferedNavigationController.m
    internal class BufferedNavigationController : UINavigationController
    {
        private readonly Queue<Action> actions = new Queue<Action>();
        private readonly INavigationStack navStack;

        private bool transitioning = false;

        public BufferedNavigationController(INavigationStack navStack): base()
        {
            this.navStack = navStack;
            this.WeakDelegate = this;

        }
            
        public override UIViewController PopViewController (bool animated)
        {
            this.actions.Enqueue(() => navStack.Pop());
            return base.PopViewController(animated);
        }

        public override UIViewController[] PopToRootViewController(bool animated)
        {
            this.actions.Enqueue(() => navStack.GotoRoot());
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
}

