using Android.App;
using Android.OS;
using Android.Views;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace RxMobile
{            
    internal class DelegatingReactiveObject : ReactiveObject 
    {
    }
        
    public abstract class RxMobileActivity<TViewModel> : ReactiveActivity, IViewFor<TViewModel>
        where TViewModel : class, INavigableViewModel, ILifecycleViewModel
    {
        private TViewModel viewModel;

        public TViewModel ViewModel
        {
            get { return viewModel; }
            set { this.RaiseAndSetIfChanged(ref viewModel, value); }
        }

        object IViewFor.ViewModel
        {
            get { return viewModel; }
            set { viewModel = (TViewModel)value; }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.WhenAnyObservable(x => x.ViewModel.Close).FirstAsync().Subscribe(_ => this.Finish());
            var app = (RxMobileApplication) this.Application;
            app.OnViewCreated(this);
        }

        protected override void OnResume()
        {
            base.OnResume();
            this.ViewModel.Resuming.Execute(null);
        }

        protected override void OnPause()
        {
            base.OnPause();
            this.ViewModel.Pausing.Execute(null);
        }

        public override void OnBackPressed()
        {
            if (!this.FragmentManager.PopBackStackImmediate())
            {
                this.ViewModel.Back.Execute(null);
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                this.ViewModel.Up.Execute(null);
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }
    }
        
    public class RxMobileActivity : Activity, IReactiveObject, IReactiveNotifyPropertyChanged<RxMobileActivity>, IHandleObservableErrors
    {
        private readonly ReactiveObject deleg = new DelegatingReactiveObject();

        public event PropertyChangingEventHandler PropertyChanging
        {
            add { deleg.PropertyChanging += value; }
            remove { deleg.PropertyChanging -= value; }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { deleg.PropertyChanged += value; }
            remove { deleg.PropertyChanged -= value; }
        }

        public IObservable<IReactivePropertyChangedEventArgs<RxMobileActivity>> Changing
        {
            get { return deleg.Changing.Select(x => new ReactivePropertyChangingEventArgs<RxMobileActivity>(this, x.PropertyName));}
        }

        public IObservable<IReactivePropertyChangedEventArgs<RxMobileActivity>> Changed
        {
            get { return deleg.Changed.Select(x => new ReactivePropertyChangedEventArgs<RxMobileActivity>(this, x.PropertyName)); }
        }

        public IDisposable SuppressChangeNotifications()
        {
            return deleg.SuppressChangeNotifications();
        }

        public void RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            (deleg as IReactiveObject).RaisePropertyChanging(args);
        }

        public void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            (deleg as IReactiveObject).RaisePropertyChanged(args);
        }

        public IObservable<Exception> ThrownExceptions { get { return deleg.ThrownExceptions; } }
    }
}

