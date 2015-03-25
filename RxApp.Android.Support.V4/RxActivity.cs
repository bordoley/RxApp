using System;

using Android.Support.V4.App;
using Android.Views;
using Android.OS;

namespace RxApp.Android
{
    public abstract class RxFragmentActivity<TViewModel> : FragmentActivity, IRxActivity<TViewModel>
        where TViewModel : INavigationViewModel
    {
        private readonly RxActivityHelper<TViewModel> helper;

        protected RxFragmentActivity()
        {
            helper = RxActivityHelper<TViewModel>.Create(this);
        }

        public IObservable<IMenuItem> OptionsItemSelected { get { return helper.OptionsItemSelected; } }
            
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

