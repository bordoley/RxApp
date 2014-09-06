using System;
using System.Reactive.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using ReactiveUI;

namespace RxApp
{        
    public interface IRxActivity
    {
        void OnCreate(Bundle bundle);

        void OnDestroy();

        void OnResume();

        void OnPause();

        void OnBackPressed();

        bool OnOptionsItemSelected(IMenuItem item);
    }
        
    public interface IRxActivity<TViewModel> : IViewFor<TViewModel>, IRxActivity
        where TViewModel : class, IMobileViewModel
    {
    }

    public static class RxActivity<TViewModel>
        where TViewModel : class, IMobileViewModel
    {
        public static IRxActivity<TViewModel> Create(Activity activity)
        {
            // FIXME: Preconditions/Contracts
            return new RxActivityImpl(activity);
        }

        // Inheritance kind of evil, but this is a super hidden class
        private sealed class RxActivityImpl : ReactiveUI.ReactiveObject, IRxActivity<TViewModel>
        {
            private readonly Activity activity;
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
                set { viewModel = (TViewModel)value; }
            }

            public void OnCreate(Bundle bundle)
            {
                // FIXME: Dispose the subscription in on destroy
                this.WhenAnyObservable(x => x.ViewModel.Close).FirstAsync().Subscribe(_ => activity.Finish());
                var app = (IRxAndroidApplication) activity.Application;
                app.OnViewCreated(this);
            }

            public void OnDestroy()
            {
            }

            public void OnResume()
            {
                ((ILifecycleViewModel) this.ViewModel).Resuming.Execute(null);
            }

            public void OnPause()
            {
                ((ILifecycleViewModel) this.ViewModel).Pausing.Execute(null);
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