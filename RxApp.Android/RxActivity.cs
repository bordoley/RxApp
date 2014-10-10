﻿using Android.App;
using Android.OS;
using Android.Views;

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;

using ReactiveUI;


namespace RxApp
{   
    public sealed class RxActivityHelper<TViewModel> : INotifyPropertyChanged, IViewFor<TViewModel>
        where TViewModel: class, INavigableViewModel, IServiceViewModel
    {
        public static RxActivityHelper<TViewModel> Create(IRxActivity activity)
        {
            Contract.Requires(activity != null);
            return new RxActivityHelper<TViewModel>(activity);
        }

        private readonly IReactiveObject notify = ReactiveObject.Create();
        private readonly IRxActivity activity;
        private TViewModel viewModel;

        private RxActivityHelper(IRxActivity activity)
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

        public event PropertyChangedEventHandler PropertyChanged
        {
            add 
            { 
                helper.PropertyChanged += value; 
            }

            remove 
            { 
                helper.PropertyChanged -= value; 
            }
        }
            
        public TViewModel ViewModel
        {
            get
            {
                return helper.ViewModel;
            }

            set
            {
                helper.ViewModel = value;
            }
        }

        object IViewFor.ViewModel
        {
            get
            {
                return ((IViewFor)helper).ViewModel;
            }

            set
            {
                ((IViewFor)helper).ViewModel = value;
            }
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