using System;

using Android.Support.V4.App;

namespace RxApp.Android
{
    public abstract class RxSupportFragment<TViewModel> : Fragment, IViewFor<TViewModel>
        where TViewModel : IActivationViewModel
    {
        public TViewModel ViewModel { get; set; }

        object IViewFor.ViewModel
        {
            get { return this.ViewModel; }

            set { this.ViewModel = (TViewModel) value; }
        }

        public override void OnResume()
        {
            base.OnResume();
            this.ViewModel.Activate.Execute();
        }

        public override void OnPause()
        {
            this.ViewModel.Deactivate.Execute();
            base.OnPause();
        }
    } 
}

