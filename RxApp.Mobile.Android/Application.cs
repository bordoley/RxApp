using ReactiveUI;
using Android.App;
using System;

namespace RxApp
{
    public interface IRxAndroidApplication : IService
    {
        void OnCreate();
        void OnTerminate();
        void OnViewCreated(IViewFor view);
    }

    public static class AndroidApplication
    {
        public static IRxAndroidApplication Create(
            INavigationStack<IMobileModel> navStack, 
            Application androidApplication, 
            IInitializableService mobileApplication,
            Action<IViewFor,IMobileViewModel> setViewModel)
        {
            return new RxAndroidApplicationImpl(
                navStack,
                androidApplication, 
                mobileApplication,
                setViewModel);
        }

        private sealed class RxAndroidApplicationImpl : IRxAndroidApplication 
        {
            private readonly INavigationStack<IMobileModel> navStack;
            private readonly Application androidApplication;
            private readonly IInitializableService mobileApplication;
            private readonly Action<IViewFor,IMobileViewModel> setViewModel;

            // FIXME: Test if this can be immutable
            private IActivityLifecycleEvents events = null;

            private int activities = 0;

            internal RxAndroidApplicationImpl(
                INavigationStack<IMobileModel> navStack, 
                Application androidApplication, 
                IInitializableService mobileApplication,
                Action<IViewFor,IMobileViewModel> setViewModel)
            {
                this.navStack = navStack;
                this.androidApplication = androidApplication;
                this.mobileApplication = mobileApplication;
                this.setViewModel = setViewModel;
            }

            public void OnCreate()
            {
                // FIXME: Its debatable if the events should be in this class at all
                events = ActivityLifecycleEvents.Register(this.androidApplication);
                events.Created.Subscribe(a => activities++);
                events.Destroyed.Subscribe(_ =>
                    {
                        activities--;
                        if (activities <= 0)
                        {
                            this.Stop();
                        }
                    });

                mobileApplication.Initialize();

                // FIXME: Should not be need after ReactiveUI 6.0.8
                ReactiveUI.RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => AndroidScheduler.UIScheduler());
            }

            public void Start()
            {
                mobileApplication.Start();
            }

            public void Stop()
            {
                mobileApplication.Stop();
            }

            public void OnTerminate()
            {
                mobileApplication.Dispose();
            }

            public void OnViewCreated(IViewFor view)
            {
                if (navStack.Current != null)
                {
                    setViewModel(view, navStack.Current);
                }
                else
                {
                    throw new Exception("View created when no model available");
                }
            }
        }
    }
}