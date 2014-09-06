using ReactiveUI;
using Android.App;
using System;

namespace RxApp
{
    public abstract class RxAndroidApplicationBase : Application, IRxAndroidApplication 
    {
        public RxAndroidApplicationBase(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {

        }

        protected abstract IRxAndroidApplication Delegate { get; }

        public override void OnCreate()
        {
            base.OnCreate();
            Delegate.OnCreate();
        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            Delegate.OnTerminate();
        }

        public void OnViewCreated(IViewFor view)
        {
            Delegate.OnViewCreated(view);
        }

        public void Start()
        {
            Delegate.Start();
        }

        public void Stop()
        {
            Delegate.Stop();
        }
    }

    public interface IRxAndroidApplication : IService
    {
        void OnCreate();
        void OnTerminate();
        void OnViewCreated(IViewFor view);
    }

    public static class RxAndroidApplication
    {
        public static IRxAndroidApplication Create(
            INavigationStackViewModel<IMobileModel> navStack, 
            Application androidApplication, 
            Func<INavigationViewController> controllerProvider,
            Action<IViewFor,IMobileViewModel> setViewModel)
        {
            return new RxAndroidApplicationImpl(
                navStack,
                androidApplication, 
                controllerProvider,
                setViewModel);
        }

        private sealed class RxAndroidApplicationImpl : IRxAndroidApplication 
        {
            private readonly INavigationStackViewModel<IMobileModel> navStack;
            private readonly Application androidApplication;
            private readonly Func<INavigationViewController> controllerProvider;
            private readonly Action<IViewFor,IMobileViewModel> setViewModel;

            // FIXME: Test if this can be immutable
            private IActivityLifecycleEvents events = null;

            private int activities = 0;

            private IDisposableService navStackService = null;

            internal RxAndroidApplicationImpl(
                INavigationStackViewModel<IMobileModel> navStack, 
                Application androidApplication, 
                Func<INavigationViewController> controllerProvider,
                Action<IViewFor,IMobileViewModel> setViewModel)
            {
                this.navStack = navStack;
                this.androidApplication = androidApplication;
                this.controllerProvider = controllerProvider;
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

                // FIXME: Should not be need after ReactiveUI 6.0.8
                ReactiveUI.RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => AndroidScheduler.UIScheduler());
                navStackService = navStack.Bind(controllerProvider);
            }

            public void Start()
            {
                navStackService.Start();
            }

            public void Stop()
            {
                navStackService.Stop();
            }

            public void OnTerminate()
            {
                navStackService.Dispose();
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