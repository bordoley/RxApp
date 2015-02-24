using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Android.App;
using Android.OS;
using Android;
using Android.Views;

using Android.Support.V4.App;
using Android.Support.V7.App;

using AndroidResource = Android.Resource;

namespace RxApp.Android
{   
    public sealed class RxActivityHelper<TViewModel>
        where TViewModel: INavigationViewModel
    {
        public static RxActivityHelper<TViewModel> Create(IRxActivity activity)
        {
            Contract.Requires(activity != null);
            return new RxActivityHelper<TViewModel>(activity);
        }

        // FIXME: I'd rather actually allow two way bindings to the menuitems in setup so that
        // they can be disabled/hidden etc.
        private readonly Subject<IMenuItem> optionsItemSelected = new Subject<IMenuItem>();

        private readonly IRxActivity activity;
        private TViewModel viewModel;

        private RxActivityHelper(IRxActivity activity)
        {
            this.activity = activity;
        }

        public IObservable<IMenuItem> OptionsItemSelected { get { return optionsItemSelected; } }

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
            this.ViewModel.Activate.Execute();
        }

        public void OnPause()
        {
            this.ViewModel.Deactivate.Execute();
        }

        public void OnBackPressed()
        {
            if (!activity.FragmentManager.PopBackStackImmediate())
            {
                this.ViewModel.Back.Execute();
            }
        }

        public bool OnOptionsItemSelected(IMenuItem item)
        {
            Contract.Requires(item != null);

            if (item.ItemId == AndroidResource.Id.Home)
            {
                // We own this one
                this.ViewModel.Up.Execute();

            } else { optionsItemSelected.OnNext(item); }

            return true;
        }
    }
        
    public abstract class RxActivity<TViewModel> : Activity, IRxActivity<TViewModel>
        where TViewModel : INavigationViewModel
    {
        private readonly RxActivityHelper<TViewModel> helper;

        protected RxActivity()
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

    public abstract class RxActionBarActivity<TViewModel> : ActionBarActivity, IRxActivity<TViewModel>
        where TViewModel : INavigationViewModel
    {
        private readonly RxActivityHelper<TViewModel> helper;

        protected RxActionBarActivity()
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
