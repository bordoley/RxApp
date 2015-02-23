using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;

using Foundation;
using UIKit;

namespace RxApp.iOS
{
    public sealed class RxUIViewControllerHelper<TViewModel>
        where TViewModel: INavigationModel, IServiceViewModel
    {
        public static RxUIViewControllerHelper<TViewModel> Create()
        {
            return new RxUIViewControllerHelper<TViewModel>();
        }
       
        private TViewModel viewModel;

        private RxUIViewControllerHelper()
        {
        }

        public TViewModel ViewModel
        {
            get 
            { 
                return viewModel; 
            }

            set 
            { 
                this.viewModel = value;
            }
        }

        public void ViewDidAppear(bool animated)
        {
            viewModel.Start.Execute();
        }

        public void ViewDidDisappear(bool animated)
        {
            viewModel.Stop.Execute();
        }
    }

    public class RxUIViewController<TViewModel>: UIViewController, IViewFor<TViewModel> 
        where TViewModel: INavigationModel, IServiceViewModel
    {
        private readonly RxUIViewControllerHelper<TViewModel> helper = RxUIViewControllerHelper<TViewModel>.Create();

        protected RxUIViewController() : base()
        {
        }

        protected RxUIViewController(NSCoder c) : base(c)
        {
        }

        protected RxUIViewController(NSObjectFlag f) : base(f)
        {
        }

        protected RxUIViewController(IntPtr handle) : base(handle)
        {
        }

        protected RxUIViewController(string nibNameOrNull, NSBundle nibBundleOrNull) : base(nibNameOrNull, nibBundleOrNull)
        {
        }

        public TViewModel ViewModel 
        {
            get { return helper.ViewModel; }

            set { helper.ViewModel = value; }
        }

        object IViewFor.ViewModel
        {
            get { return helper.ViewModel; }

            set { this.ViewModel = (TViewModel) value; }
        }

        public override void ViewDidAppear(bool animated)
        {
            helper.ViewDidAppear(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            helper.ViewDidDisappear(animated);
        }
    }
}

