﻿using MonoTouch.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using MonoTouch.UIKit;

namespace RxApp
{
    public sealed class RxUIApplicationDelegateHelper
    {
        public static RxUIApplicationDelegateHelper Create(
            INavigationStack navStack,
            IService applicationService,
            Func<object, IDisposable> provideController,
            Func<object, UIViewController> provideView)
        {
            return new RxUIApplicationDelegateHelper(navStack, applicationService, provideController, provideView);
        }

        private readonly IDictionary<object, UIViewController> views = new Dictionary<object, UIViewController>();

        private readonly INavigationStack navStack;
        private readonly IService applicationService;
        private readonly Func<object, IDisposable> provideController;
        private readonly Func<object, UIViewController> provideView;

        private CompositeDisposable subscription = null;
        private UIWindow window;

        private RxUIApplicationDelegateHelper(
            INavigationStack navStack,
            IService applicationService,
            Func<object, IDisposable> provideController,
            Func<object, UIViewController> provideView)
        {
            this.navStack = navStack;
            this.applicationService = applicationService;
            this.provideController = provideController;
            this.provideView = provideView;
        }

        public bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var navController = new NavigationController(navStack);
            window = new UIWindow(UIScreen.MainScreen.Bounds);
            window.RootViewController = navController;

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
                            applicationService.Stop();
                        }
                        else if (newHead != null && !views.ContainsKey(newHead))
                        {
                            var view = provideView(newHead);
                            views[newHead] = view;
                            navController.PushViewController(view, true);
                        }

                        foreach (var model in removed)
                        {
                            IDisposable view = views[model];
                            views.Remove(model);
                            view.Dispose();
                        }
                    }));

            subscription.Add(navStack.BindController(provideController));

            window.MakeKeyAndVisible();
            applicationService.Start();
            return true;
        }

        public void WillTerminate(UIApplication app)
        {
            applicationService.Stop();
            subscription.Dispose();
        }
    }


    internal class NavigationController : UINavigationController
    {
        private readonly INavigationStack navStack;

        public NavigationController(INavigationStack navStack): base()
        {
            this.navStack = navStack;
        }

        public override UIViewController PopViewControllerAnimated (bool animated)
        {
            var retval = base.PopViewControllerAnimated(animated);
            navStack.Pop();
            return retval;
        }
    }
}

