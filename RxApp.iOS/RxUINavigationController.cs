using System;
using UIKit;
using System.Collections.Generic;
using Foundation;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace RxApp.iOS
{
    // See: https://github.com/Plasma/BufferedNavigationController/blob/master/BufferedNavigationController.m
    public sealed class RxUINavigationController : UINavigationController
    {
        private readonly Subject<Action> actions = new Subject<Action>();

        private readonly BehaviorSubject<bool> transitioning = new BehaviorSubject<bool>(false);

        public RxUINavigationController(): base()
        {
            base.Delegate = new RxUINavigationControllerDelegate(null);

            // OK to leak the subscription
            actions
                .Delay(_ => transitioning.Where(x => !x))
                .Subscribe(x => x());

            this.DidShowViewController += (o, e) => 
            { 
                this.transitioning.OnNext(false); 
            };

            this.WillShowViewController += (o,e) =>  
            {
                this.transitioning.OnNext(true);

                var transitionCoordinator = this.TopViewController.GetTransitionCoordinator();
                if (transitionCoordinator != null)
                {
                    transitionCoordinator.NotifyWhenInteractionEndsUsingBlock(ctx =>
                        {
                            if (ctx.IsCancelled) { this.transitioning.OnNext(false); }
                        });
                }
            };
        }

        public event EventHandler<Tuple<UIViewController,bool>> DidShowViewController;

        public event EventHandler<Tuple<UIViewController,bool>> WillShowViewController;

        public override sealed NSObject WeakDelegate
        {
            get { return base.WeakDelegate; }
            set
            { 
                if (value is IUINavigationControllerDelegate)
                {
                    var deleg = new RxUINavigationControllerDelegate(value as IUINavigationControllerDelegate);
                    base.WeakDelegate = deleg;
                }
                else
                {
                    throw new ArgumentException("value must implement IUINavigationControllerDelegate");
                }
            }
        }
      
        public override sealed UIViewController PopViewController(bool animated)
        {
            var viewFor = (IViewFor)this.TopViewController;
            var viewModel = (INavigationViewModel)viewFor.ViewModel;
            this.actions.OnNext(() =>
                {
                    base.PopViewController(animated);
                    viewModel.Back.Execute();
                });
            return null;
        }

        public override sealed void SetViewControllers(UIViewController[] controllers, bool animated)
        {
            this.actions.OnNext(() => base.SetViewControllers(controllers, animated));
        }

        public override sealed UIViewController[] PopToRootViewController(bool animated)
        {
            throw new NotSupportedException();
        }

        public override sealed UIViewController[] PopToViewController(UIViewController viewController, bool animated)
        {
            return null;
        }

        public override sealed void PushViewController(UIViewController viewController, bool animated)
        {
            throw new NotSupportedException();
        }

        private sealed class RxUINavigationControllerDelegate : UINavigationControllerDelegate
        {
            private readonly IUINavigationControllerDelegate deleg;

            internal RxUINavigationControllerDelegate(IUINavigationControllerDelegate deleg)
            {
                this.deleg = deleg;
            }

            public override void DidShowViewController(
                UINavigationController navigationController, 
                UIViewController viewController, 
                bool animated)
            {
                (navigationController as RxUINavigationController).DidShowViewController(navigationController, Tuple.Create(viewController, animated));

                if (deleg != null) 
                { 
                    deleg.DidShowViewController(navigationController, viewController, animated); 
                }
            }

            public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForOperation(
                UINavigationController navigationController, 
                UINavigationControllerOperation operation, 
                UIViewController fromViewController, 
                UIViewController toViewController)
            {
                if (deleg != null)
                {
                    return deleg.GetAnimationControllerForOperation(navigationController, operation, fromViewController, toViewController);
                }

                return null;
            }

            public override IUIViewControllerInteractiveTransitioning GetInteractionControllerForAnimationController(
                UINavigationController navigationController, 
                IUIViewControllerAnimatedTransitioning animationController)
            {
                if (deleg != null)
                {
                    return deleg.GetInteractionControllerForAnimationController(navigationController, animationController);
                }

                return null;
            }

            public override UIInterfaceOrientation GetPreferredInterfaceOrientation(UINavigationController navigationController)
            {
                if (deleg != null)
                {
                    return deleg.GetPreferredInterfaceOrientation(navigationController);
                }

                return UIInterfaceOrientation.Unknown;
            }

            public override UIInterfaceOrientationMask SupportedInterfaceOrientations(UINavigationController navigationController)
            {
                if (deleg != null)
                {
                    return deleg.SupportedInterfaceOrientations(navigationController);
                }

                return UIInterfaceOrientationMask.All;
            }

            public override void WillShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
            {
                (navigationController as RxUINavigationController).WillShowViewController(navigationController, Tuple.Create(viewController, animated));

                if (deleg != null)
                {
                    deleg.WillShowViewController(navigationController, viewController, animated);
                }
            }
        }
    }
}

