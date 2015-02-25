using Fragment = Android.App.Fragment;
using SupportFragment = Android.Support.V4.App.Fragment;

using RxApp;

namespace RxApp.Android
{
    public abstract class RxFragment<TViewModel> : Fragment, IViewFor<TViewModel>
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

    public abstract class RxSupportFragment<TViewModel> : SupportFragment, IViewFor<TViewModel>
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