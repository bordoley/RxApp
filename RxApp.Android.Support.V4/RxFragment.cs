using System;

using Android.Support.V4.App;

namespace RxApp.Android
{
    public abstract class RxSupportFragment<TViewModel> : Fragment, IViewFor<TViewModel>
        where TViewModel : IActivationViewModel
    {
        private TViewModel viewModel;

        public TViewModel ViewModel
        {
            get { return viewModel; }

            set { viewModel = value; }
        }

        object IViewFor.ViewModel
        {
            get { return viewModel; }

            set { this.ViewModel = (TViewModel) value; }
        }

        public override void OnResume()
        {
            viewModel.Activate.Execute();
        }

        public override void OnPause()
        {
            viewModel.Deactivate.Execute();
        }
    } 
}

