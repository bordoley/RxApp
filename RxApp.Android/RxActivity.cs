using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Android.App;
using Android.OS;
using Android;
using Android.Views;

using AndroidResource = Android.Resource;

namespace RxApp.Android
{   
    public sealed class RxActivityHelper<TActivity, TViewModel>
        where TActivity : Activity, IViewFor<TViewModel>
        where TViewModel: INavigationViewModel
    {
        public static RxActivityHelper<TActivity, TViewModel> Create(TActivity activity)
        {
            Contract.Requires(activity != null);
            return new RxActivityHelper<TActivity, TViewModel>(activity);
        }

        private readonly TActivity activity;

        private RxActivityHelper(TActivity activity)
        {
            this.activity = activity;
        }

        public void OnCreate(Bundle bundle)
        {
            var app = (IRxApplication) activity.Application;
            app.OnActivityCreated(activity);
        }

        public void OnResume()
        {
            activity.ViewModel.Activate.Execute();
        }

        public void OnPause()
        {
            activity.ViewModel.Deactivate.Execute();
        }

        public void OnBackPressed()
        {
            if (!activity.FragmentManager.PopBackStackImmediate())
            {
                activity.ViewModel.Back.Execute();
            }
        }

        public bool OnOptionsItemSelected(IMenuItem item)
        {
            Contract.Requires(item != null);

            if (item.ItemId == AndroidResource.Id.Home)
            {
                // We own this one
                activity.ViewModel.Up.Execute();
                return true;
            }

            return false;
        }
    }
        
    public abstract class RxActivity<TViewModel> : Activity, IViewFor<TViewModel>
        where TViewModel : INavigationViewModel
    {
        private readonly RxActivityHelper<RxActivity<TViewModel>,TViewModel> helper;

        private TViewModel viewModel;

        protected RxActivity()
        {
            helper = RxActivityHelper<RxActivity<TViewModel>,TViewModel>.Create(this);
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
