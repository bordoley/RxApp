using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using ReactiveUI;

namespace RxApp
{
    public sealed class RxUIViewControllerHelper<TViewModel> : INotifyPropertyChanged
        where TViewModel: class, /*INavigableViewModel,*/ IServiceViewModel
    {
        public static RxUIViewControllerHelper<TViewModel> Create()
        {
            return new RxUIViewControllerHelper<TViewModel>();
        }

        private readonly IReactiveObject notify = ReactiveObject.Create();
       
        private TViewModel viewModel;

        private RxUIViewControllerHelper()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add 
            {
                notify.PropertyChanged += value;
            }

            remove
            {
                notify.PropertyChanged -= value;
            }
        }

        public TViewModel ViewModel
        {
            get 
            { 
                return viewModel; 
            }

            set 
            { 
                notify.RaiseAndSetIfChanged(ref viewModel, value); 
            }
        }

        public void ViewDidAppear(bool animated)
        {
            viewModel.Start.Execute(null);
        }

        public void ViewDidDisappear(bool animated)
        {
            viewModel.Stop.Execute(null);
        }
    }

    public class RxUIViewController<TViewModel>: UIViewController, IViewFor<TViewModel> 
        where TViewModel: class, /*INavigableViewModel,*/ IServiceViewModel
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

        public event PropertyChangedEventHandler PropertyChanged
        {
            add 
            { 
                helper.PropertyChanged += value; 
            }

            remove 
            { 
                helper.PropertyChanged -= value; 
            }
        }

        public TViewModel ViewModel
        {
            get
            {
                return helper.ViewModel;
            }

            set
            {
                helper.ViewModel = value;
            }
        }

        object IViewFor.ViewModel
        {
            get
            {
                return helper.ViewModel;
            }

            set
            {
                this.ViewModel = (TViewModel) value;
            }
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

