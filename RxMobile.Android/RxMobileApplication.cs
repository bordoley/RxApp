using Android.App;
using Android.Content;
using Android.Runtime;
using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace RxMobile
{          


    public abstract class RxMobileApplication : Application
    {
        private IMobileApplication application = null;
        private IActivityLifecycleEvents events = null;
        private int activities = 0;

        public RxMobileApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected abstract void SetViewModel(IViewFor view, IMobileViewModel viewModel);
        protected abstract IMobileApplication CreateApplication();

        public override void OnCreate()
        {
            base.OnCreate();

            events = ActivityLifecycleEvents.Register(this);
            events.Created.Subscribe(a => activities++);
            events.Destroyed.Subscribe(_ =>
                {
                    activities--;
                    if (activities <= 0)
                    {
                        application.Dispose();
                        application = null;
                    }
                });

            RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => AndroidScheduler.UIScheduler());
        }

        public override void OnTerminate()
        {
            if (application != null)
            {
                application.Dispose();
                application = null;
            }
            base.OnTerminate();
        }

        public void OnViewCreated(IViewFor view)
        {
            if (application.ViewStack.Current != null)
            {
                this.SetViewModel(view, application.ViewStack.Current);
            }
            else
            {
                throw new Exception("RxMobileActivity created when no viewmodel available");
            }
        }

        public void Run()
        {
            application = this.CreateApplication();
            application.Run();
        }
    }
}