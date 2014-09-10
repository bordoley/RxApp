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
    public class RxActivityBase<TViewModel> : Activity, IViewFor<TViewModel>, INotifyPropertyChanged
        where TViewModel : class, IMobileViewModel
    {
        private readonly IRxActivity<TViewModel> deleg;

        protected RxActivityBase()
        {
            deleg = RxActivity.Create<TViewModel>(this);
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

        protected override void OnDestroy()
        {
            deleg.OnDestroy();
            base.OnDestroy();
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
            return deleg.OnOptionsItemSelected(item);
        }
    }

    public interface IRxActivity
    {
        void OnCreate(Bundle bundle);

        void OnDestroy();

        void OnResume();

        void OnPause();

        void OnBackPressed();

        bool OnOptionsItemSelected(IMenuItem item);
    }
        
    public interface IRxActivity<TViewModel> : IRxActivity, IViewFor<TViewModel>, INotifyPropertyChanged
        where TViewModel : class, IMobileViewModel
    {
    }

    public static class RxActivity
    {
        public static IRxActivity<TViewModel> Create<TViewModel>(Activity activity)
            where TViewModel: class, IMobileViewModel
        {
            Contract.Requires(activity != null);
            return new RxActivityImpl<TViewModel>(activity);
        }

        // Inheritance kind of evil, but this is a super hidden class
        private sealed class RxActivityImpl<TViewModel> : ReactiveUI.ReactiveObject, IRxActivity<TViewModel>
             where TViewModel: class, IMobileViewModel
        {
            private readonly Activity activity;
            private IDisposable closeSubscription;
            private TViewModel viewModel;

            internal RxActivityImpl(Activity activity)
            {
                this.activity = activity;
            }
                    
            public TViewModel ViewModel
            {
                get { return viewModel; }
                set { this.RaiseAndSetIfChanged(ref viewModel, value); }
            }

            object IViewFor.ViewModel
            {
                get { return viewModel; }
                set { this.ViewModel = (TViewModel)value; }
            }

            public void OnCreate(Bundle bundle)
            {
                closeSubscription = this.WhenAnyObservable(x => x.ViewModel.Close).FirstAsync().Subscribe(_ => activity.Finish());
                var app = (IRxAndroidApplication) activity.Application;
                app.OnViewCreated(this);
            }

            public void OnDestroy()
            {
                closeSubscription.Dispose();
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

                return activity.OnOptionsItemSelected(item);
            }
        }
    }
}