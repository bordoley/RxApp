using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;

using Android.App;
using Android.OS;
using Android.Views;

using Android.Support.V4.App;
using Android.Support.V7.App;

using ReactiveUI;

namespace RxApp
{   
    public sealed class RxActivityHelper<TViewModel>
        where TViewModel: class, INavigableViewModel, IServiceViewModel
    {
        public static RxActivityHelper<TViewModel> Create(IRxActivity activity)
        {
            Contract.Requires(activity != null);
            return new RxActivityHelper<TViewModel>(activity);
        }

        private readonly IRxActivity activity;
        private TViewModel viewModel;

        private RxActivityHelper(IRxActivity activity)
        {
            this.activity = activity;
        }

        public TViewModel ViewModel
        {
            get { return viewModel; }

            set { this.viewModel = value; }
        }

        public void OnCreate(Bundle bundle)
        {
            var app = (IRxApplication) activity.Application;
            app.OnActivityCreated(activity);
        }

        public void OnResume()
        {
            this.ViewModel.Start.Execute(null);
        }

        public void OnPause()
        {
            this.ViewModel.Stop.Execute(null);
        }

        public void OnBackPressed()
        {
            if (!activity.FragmentManager.PopBackStackImmediate())
            {
                this.ViewModel.Back.Execute(null);
            }
        }

        public bool OnOptionsItemSelected(IMenuItem item)
        {
            Contract.Requires(item != null);

            if (item.ItemId == Android.Resource.Id.Home)
            {
                this.ViewModel.Up.Execute(null);
                return true;
            }

            return false;
        }
    }
        
    public abstract class RxActivity<TViewModel> : Activity, IRxActivity<TViewModel>
        where TViewModel : class, INavigableViewModel, IServiceViewModel
    {
        private readonly RxActivityHelper<TViewModel> helper;

        protected RxActivity()
        {
            helper = RxActivityHelper<TViewModel>.Create(this);
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

        public override void OnBackPressed()
        {
            helper.OnBackPressed();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return helper.OnOptionsItemSelected(item) ? true : base.OnOptionsItemSelected(item);
        }
    }

    public abstract class RxFragmentActivity<TViewModel> : FragmentActivity, IRxActivity<TViewModel>
        where TViewModel : class, INavigableViewModel, IServiceViewModel
    {
        private readonly RxActivityHelper<TViewModel> helper;

        protected RxFragmentActivity()
        {
            helper = RxActivityHelper<TViewModel>.Create(this);
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

        public override void OnBackPressed()
        {
            helper.OnBackPressed();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return helper.OnOptionsItemSelected(item) ? true : base.OnOptionsItemSelected(item);
        }
    }

    public abstract class RxActionBarActivity<TViewModel> : ActionBarActivity, IRxActivity<TViewModel>
        where TViewModel : class, INavigableViewModel, IServiceViewModel
    {
        private readonly RxActivityHelper<TViewModel> helper;

        protected RxActionBarActivity()
        {
            helper = RxActivityHelper<TViewModel>.Create(this);
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

        public override void OnBackPressed()
        {
            helper.OnBackPressed();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return helper.OnOptionsItemSelected(item) ? true : base.OnOptionsItemSelected(item);
        }
    }
}
