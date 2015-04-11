using Android.App;
using RxApp;

namespace RxApp.Android
{
    public abstract class RxFragment<TViewModel> : Fragment, IViewFor<TViewModel>
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