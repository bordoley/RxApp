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
            var application = getApplication();

            subscription = Disposable.Compose(
                application
                    .ObserveOnMainThread()
                    .Scan(new Dictionary<INavigationViewModel, UIViewController>(), (acc, src) =>
                        {
                            var head = src.FirstOrDefault();
                            var newHeadSet = new HashSet<INavigationModel>(src);
                            var removed = acc.Keys.Where(y => !newHeadSet.Contains(y)).ToList();

                            if (!acc.ContainsKey(head))
                            {
                                var view = provideView(head);
                                acc[head] = view;
                            }

                            if (head != null)
                            {
                                navViewController.ViewModel = head;
                                var viewControllers = src.Reverse().Select(model => acc[model]).ToArray();
                                navViewController.SetViewControllers(viewControllers, true);
                            }

                            foreach (var model in removed)
                            {
                                IDisposable view = acc[model];
                                acc.Remove(model);
                                view.Dispose();
                            }

                            return acc;
                        }).Subscribe()
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
    internal class BufferedNavigationController : UINavigationController, IViewFor<INavigationViewModel>
    {
        private readonly Queue<Action> actions = new Queue<Action>();

        private bool transitioning = false;

        public BufferedNavigationController(): base()
        {
            this.WeakDelegate = this;
        }

        public INavigationViewModel ViewModel { get; set; }


        object IViewFor.ViewModel
        {
            get
            {
                return this.ViewModel;
            }
            set
            {
                this.ViewModel = (INavigationViewModel) value;
            }
        }
      
        public override UIViewController PopViewController (bool animated)
        {
            this.actions.Enqueue(() => 
                {
                    this.ViewModel.Back.Execute();
                });
            return base.PopViewController(animated);
        }

        public override UIViewController[] PopToRootViewController(bool animated)
        {
            this.actions.Enqueue(() => 
                {
                    this.ViewModel.Up.Execute();
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

