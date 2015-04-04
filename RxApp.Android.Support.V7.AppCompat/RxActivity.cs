using System;
using Android.Support.V7.App;
using Android.Views;
using Android.OS;

namespace RxApp.Android
{
    public abstract class RxActionBarActivity<TViewModel> : ActionBarActivity, IViewFor<TViewModel>
        where TViewModel : INavigationViewModel
    {
        private readonly RxActivityHelper<RxActionBarActivity<TViewModel>,TViewModel> helper;
        private TViewModel viewModel;

        protected RxActionBarActivity()
        {
            helper = RxActivityHelper<RxActionBarActivity<TViewModel>,TViewModel>.Create(this);
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

