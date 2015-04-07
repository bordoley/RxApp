using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;

using Foundation;
using UIKit;

namespace RxApp.iOS
{
    public sealed class RxUIViewControllerHelper<TViewController,TViewModel>
        where TViewController : UIViewController, IReadOnlyViewFor<TViewModel> 
        where TViewModel: INavigationViewModel
    {
        public static RxUIViewControllerHelper<TViewController,TViewModel> Create(TViewController viewController)
        {
            return new RxUIViewControllerHelper<TViewController,TViewModel>(viewController);
        }

        private readonly TViewController viewController;

        private RxUIViewControllerHelper(TViewController viewController)
        {
            this.viewController = viewController;
        }

        public void ViewDidAppear(bool animated)
        {
            viewController.ViewModel.Activate.Execute();
        }

        public void ViewDidDisappear(bool animated)
        {
            viewController.ViewModel.Deactivate.Execute();
        }
    }

    public abstract class RxUIViewController<TViewModel>: UIViewController, IViewFor<TViewModel>, IReadOnlyViewFor<TViewModel> 
        where TViewModel: INavigationViewModel
    {
        private readonly RxUIViewControllerHelper<RxUIViewController<TViewModel>,TViewModel> helper;

        protected RxUIViewController() : base()
        {
            helper = RxUIViewControllerHelper<RxUIViewController<TViewModel>,TViewModel>.Create(this);
        }

        protected RxUIViewController(NSCoder c) : base(c)
        {
            helper = RxUIViewControllerHelper<RxUIViewController<TViewModel>,TViewModel>.Create(this);
        }

        protected RxUIViewController(NSObjectFlag f) : base(f)
        {
            helper = RxUIViewControllerHelper<RxUIViewController<TViewModel>,TViewModel>.Create(this);
        }

        protected RxUIViewController(IntPtr handle) : base(handle)
        {
            helper = RxUIViewControllerHelper<RxUIViewController<TViewModel>,TViewModel>.Create(this);
        }

        protected RxUIViewController(string nibNameOrNull, NSBundle nibBundleOrNull) : base(nibNameOrNull, nibBundleOrNull)
        {
            helper = RxUIViewControllerHelper<RxUIViewController<TViewModel>,TViewModel>.Create(this);
        }

        public TViewModel ViewModel { get; set; }

        object IViewFor.ViewModel
        {
            get { return this.ViewModel; }

            set { this.ViewModel = (TViewModel) value; }
        }

        object IReadOnlyViewFor.ViewModel
        {
            get { return this.ViewModel; }
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            helper.ViewDidAppear(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            helper.ViewDidDisappear(animated);
            base.ViewDidDisappear(animated);
        }
    }
}

