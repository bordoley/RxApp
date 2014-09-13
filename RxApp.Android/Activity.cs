using Android.App;
using Android.OS;
using Android.Views;

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;

using ReactiveUI;


namespace RxApp
{   
    public interface IActivity 
    {
        Application Application { get; }
        FragmentManager FragmentManager { get; }
        void Finish();
        bool OnOptionsItemSelected(IMenuItem item);
    }

    public interface IRxActivity : IActivity, IViewFor, INotifyPropertyChanged
    {

    }

    public interface IRxActivity<TViewModel> : IRxActivity, IViewFor<TViewModel>
        where TViewModel : class, IMobileViewModel
    {
    }
        
    public abstract class RxActivity<TViewModel> : Activity, IRxActivity<TViewModel>
        where TViewModel : class, IMobileViewModel
    {
        private readonly RxActivityDelegate<TViewModel> deleg;

        protected RxActivity()
        {
            deleg = RxActivityDelegate<TViewModel>.Create(this);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { deleg.PropertyChanged += value; }
            remove { deleg.PropertyChanged -= value; }
        }
            
        public TViewModel ViewModel
        {
            get
            {
                return deleg.ViewModel;
            }

            set
            {
                deleg.ViewModel = value;
            }
        }

        object IViewFor.ViewModel
        {
            get
            {
                return ((IViewFor)deleg).ViewModel;
            }

            set
            {
                ((IViewFor)deleg).ViewModel = value;
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            deleg.OnCreate(bundle);
        }

        protected override void OnResume()
        {
            base.OnResume();
            deleg.OnResume();
        }

        protected override void OnPause()
        {
            deleg.OnPause();
            base.OnPause();
        }

        public override void OnBackPressed()
        {
            deleg.OnBackPressed();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return deleg.OnOptionsItemSelected(item) ? true : base.OnOptionsItemSelected(item);
        }
    }
        
    public sealed class RxActivityDelegate<TViewModel> : INotifyPropertyChanged, IViewFor<TViewModel>
        where TViewModel: class, IMobileViewModel
    {
        public static RxActivityDelegate<TViewModel> Create(IRxActivity activity)
        {
            Contract.Requires(activity != null);
            return new RxActivityDelegate<TViewModel>(activity);
        }

        private readonly IReactiveObject notify = ReactiveObject.Create();
        private readonly IRxActivity activity;
        private TViewModel viewModel;

        private RxActivityDelegate(IRxActivity activity)
        {
            this.activity = activity;
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add 
            {
                notify.PropertyChanged += value;
            }

            remove
            {
                notify.PropertyChanged -= value;
            }
        }
                
        public TViewModel ViewModel
        {
            get 
            { 
                return viewModel; 
            }

            set 
            { 
                notify.RaiseAndSetIfChanged(ref viewModel, value); 
            }
        }

        object IViewFor.ViewModel
        {
            get 
            { 
                return viewModel; 
            }

            set 
            { 
                this.ViewModel = (TViewModel)value; 
            }
        }

        public void OnCreate(Bundle bundle)
        {
            var app = (IRxAndroidApplication) activity.Application;
            app.OnActivityCreated(activity);
        }

        public void OnResume()
        {
            ((IServiceViewModel) this.ViewModel).Start.Execute(null);
        }

        public void OnPause()
        {
            ((IServiceViewModel) this.ViewModel).Stop.Execute(null);
        }

        public void OnBackPressed()
        {
            if (!activity.FragmentManager.PopBackStackImmediate())
            {
                ((INavigableViewModel) this.ViewModel).Back.Execute(null);
            }
        }

        public bool OnOptionsItemSelected(IMenuItem item)
        {
            Contract.Requires(item != null);

            if (item.ItemId == Android.Resource.Id.Home)
            {
                ((INavigableViewModel) this.ViewModel).Up.Execute(null);
                return true;
            }

            return false;
        }
    }
}
