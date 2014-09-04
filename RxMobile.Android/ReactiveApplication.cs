﻿using Android.App;
using Android.Content;
using Android.Runtime;
using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace RxMobile
{           
    public abstract class RxMobileApplication : Application
    {
        static public void PresentView(Func<INavigableViewModel, Type> provideViewType, Context context, INavigableViewModel vieWModel)
        {
            // FIXME: Precondition or Contract checks
            var viewType = provideViewType(vieWModel);
            var intent = new Intent(context, viewType).SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
            context.StartActivity(intent);
        }

        private readonly IViewStack viewStack = ViewStack.Create();

        private IMobileApplication application = null;
        private IService viewStackBinder = null;

        private IDisposable viewStackSubscription = null;

        public RxMobileApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected abstract void SetActivityViewModel(Activity activity, INavigableViewModel viewModel);
        protected abstract IMobileApplication CreateApplication(IViewStack viewStack);

        public override void OnCreate()
        {
            base.OnCreate();

            RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => AndroidScheduler.UIScheduler());

            viewStackSubscription =
                viewStack
                    .WhenAnyValue(x => x.Current)
                    .Buffer(2, 1)
                    .Select(x => Tuple.Create(x[0], x[1]))
                    .Where(t => (t.Item1 != null) && (t.Item2 == null))
                    .Subscribe(_ => this.OnPause());
        }

        public override sealed void OnTerminate()
        {
            viewStackSubscription.Dispose();
            viewStackSubscription = null;
            base.OnTerminate();
        }

        public void OnActivityCreated(Activity activity)
        {
            if (viewStack.Current != null)
            {
                this.SetActivityViewModel(activity, viewStack.Current);
            }
            else
            {
                throw new Exception("ReactiveApplicationActivity created when no viewmodel available");
            }
        }

        public void OnResume()
        {
            application = this.CreateApplication(viewStack);
            viewStackBinder = 
                ViewStackBinder.Create(viewStack, application.PresentView, application.ProvideController);

            viewStackBinder.Start();
            application.Start();
        }

        public void OnPause()
        {
            application.Stop();
            application = null;

            viewStackBinder.Stop();
            viewStackBinder = null;
        }
    }
}