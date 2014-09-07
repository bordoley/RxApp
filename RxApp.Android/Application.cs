using ReactiveUI;
using Android.App;
using System;
using System.Reactive.Linq;

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
            Func<IModelBinder<IMobileModel>> binderProvider)
        {
            return new RxAndroidApplicationImpl(navStack, binderProvider);
        }

        private sealed class RxAndroidApplicationImpl : IRxAndroidApplication 
        {
            private readonly INavigationStackViewModel<IMobileModel> navStack;
            private readonly Func<IModelBinder<IMobileModel>> binderProvider;

            private IDisposableService navStackService = null;

            internal RxAndroidApplicationImpl(
                INavigationStackViewModel<IMobileModel> navStack, 
                Func<IModelBinder<IMobileModel>> binderProvider)
            {
                this.navStack = navStack;
                this.binderProvider = binderProvider;
            }

            public void OnCreate()
            {
                // FIXME: release the subscription onTerminate.
                navStack
                    .WhenAnyValue(x => x.Current)
                    .Buffer(2, 1)
                    .Subscribe(models =>
                        {
                            if (models[0] != null && models[1] == null)
                            {
                                this.Stop();
                            }
                        });

                // FIXME: Should not be need after ReactiveUI 6.0.8
                ReactiveUI.RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => AndroidScheduler.UIScheduler());
                navStackService = navStack.Bind(binderProvider);
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
                    view.ViewModel = navStack.Current;
                }
                else
                {
                    throw new Exception("View created when no model available");
                }
            }
        }
    }
}