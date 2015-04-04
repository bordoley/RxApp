using System;

using Android.Support.V4.App;
using Android.Views;
using Android.OS;

namespace RxApp.Android
{
    public abstract class RxFragmentActivity<TViewModel> : FragmentActivity, IViewFor<TViewModel>
        where TViewModel : INavigationViewModel
    {
        private readonly RxActivityHelper<RxFragmentActivity<TViewModel>, TViewModel> helper;

        private TViewModel viewModel;

        protected RxFragmentActivity()
        {
            helper = RxActivityHelper<RxFragmentActivity<TViewModel>, TViewModel>.Create(this);
        }

        public TViewModel ViewModel
        {
            get { return this.viewModel; }

            set { this.viewModel = value; }
        }

        object IViewFor.ViewModel
        {
            get { return this.ViewModel; }

            set { this.ViewModel = (TViewModel) value; }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            helper.OnCreate(bundle);
        }

        protected override void OnResume()
        {
            base.OnResume();
            helper.OnResume();
        }

        protected override void OnPause()
        {
            helper.OnPause();
            base.OnPause();
        }

        public override sealed void OnBackPressed()
        {
            helper.OnBackPressed();
        }

        public override sealed bool OnOptionsItemSelected(IMenuItem item)
        {
            return helper.OnOptionsItemSelected(item);
        }
    }
}

