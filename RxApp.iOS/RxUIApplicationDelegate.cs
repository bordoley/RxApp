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
            Func<IObservable<IEnumerable<INavigationModel>>> getApplication,
            Func<INavigationViewModel, UIViewController> provideView)
        {
            return new RxUIApplicationDelegateHelper(getApplication, provideView);
        }

        private readonly Func<IObservable<IEnumerable<INavigationModel>>> getApplication;
        private readonly Func<INavigationViewModel, UIViewController> provideView;

        private IDisposable subscription;

        private RxUIApplicationDelegateHelper(
            Func<IObservable<IEnumerable<INavigationModel>>> getApplication,
            Func<INavigationViewModel, UIViewController> provideView)
        {
            this.getApplication = getApplication;
            this.provideView = provideView;
        }

        public bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var navViewController = new BufferedNavigationController();
            var application = getApplication();
            var views = new Dictionary<INavigationViewModel, UIViewController>();

            subscription = application
                .Scan(Tuple.Create<IEnumerable<INavigationModel>, IEnumerable<INavigationModel>>(Enumerable.Empty<INavigationModel>(), Enumerable.Empty<INavigationModel>()), (acc, next) =>
                    {
                        return Tuple.Create(acc.Item2, next);
                    })
                .Subscribe(x =>
                    {
                        var newHead = x.Item2.LastOrDefault();
    
                        var newHeadSet = new HashSet<INavigationModel>(x.Item2);
                        var removed = x.Item1.Where(y => !newHeadSet.Contains(y));

                        if (!views.ContainsKey(newHead))
                        {
                            var view = provideView(newHead);
                            views[newHead] = view;
                        }

                        navViewController.ViewModel = newHead;
                        var viewControllers = x.Item2.Reverse().Select(model => 
                            views[model]).ToArray();
                        navViewController.SetViewControllers(viewControllers, true);

                        foreach (var model in removed)
                        {
                            IDisposable view = views[model];
                            views.Remove(model);
                            view.Dispose();
                        }
                    });

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
                    this.GetApplication,
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

        protected abstract IObservable<IEnumerable<INavigationModel>> GetApplication();

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

