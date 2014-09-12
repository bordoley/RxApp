using Android.App;
using ReactiveUI;
using System;
using System.Diagnostics.Contracts;
using System.Reactive;
using System.Reactive.Linq;

namespace RxApp
{
    public abstract class RxAndroidApplicationBase : Application, IRxAndroidApplication 
    {
        private readonly INavigationStackModel<IMobileModel> navigationStack;
        private readonly IRxAndroidApplication deleg;

        public RxAndroidApplicationBase(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            navigationStack = RxApp.NavigationStack.Create<IMobileModel>();
            deleg = RxAndroidApplication.Create(navigationStack, () => 
                AndroidModelBinder.Create(this.ApplicationContext, ProvideControllerBinder(), model => GetViewType(model)));
        }

        protected abstract Type GetViewType(IMobileViewModel model);

        protected abstract IModelBinder<IMobileControllerModel> ProvideControllerBinder();

        public INavigationStackModel<IMobileModel> NavigationStack
        {
            get
            {
                return navigationStack;
            }
        }
                    
        public override void OnCreate()
        {
            base.OnCreate();
            deleg.OnCreate();
        }

        public override void OnTerminate()
        {
            deleg.OnTerminate();
            base.OnTerminate();
        }

        public void OnViewCreated(IViewFor view)
        {
            deleg.OnViewCreated(view);
        }

        public void Start()
        {
            deleg.Start();
        }

        public void Stop()
        {
            deleg.Stop();
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
            Contract.Requires(navStack != null);
            Contract.Requires(binderProvider != null);

            return new RxAndroidApplicationImpl(navStack, binderProvider);
        }

        private sealed class RxAndroidApplicationImpl : IRxAndroidApplication 
        {
            private readonly INavigationStackViewModel<IMobileModel> navStack;
            private readonly Func<IModelBinder<IMobileModel>> binderProvider;

            private IDisposableService navStackService = null;
            private IDisposable navStackSubscription = null;

            internal RxAndroidApplicationImpl(
                INavigationStackViewModel<IMobileModel> navStack, 
                Func<IModelBinder<IMobileModel>> binderProvider)
            {
                this.navStack = navStack;
                this.binderProvider = binderProvider;
            }

            public void OnCreate()
            {
                navStackSubscription = 
                    Observable
                        .FromEventPattern<NotifyNavigationStackChangedEventArgs<IMobileModel>>(navStack, "NavigationStackChanged")
                        .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs<IMobileModel>> e) => 
                            {
                                var newHead = e.EventArgs.NewHead;
                                var oldHead = e.EventArgs.OldHead;

                                if (oldHead != null && newHead == null)
                                {
                                    this.Stop();
                                }
                            });

                // FIXME: Should not be need after ReactiveUI 6.0.8
                ReactiveUI.RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => AndroidScheduler.UIScheduler());
                navStackService = navStack.Bind(binderProvider);
            }
                
            public void OnTerminate()
            {
                navStackService.Dispose();
                navStackSubscription.Dispose();
            }

            public void Start()
            {
                navStackService.Start();
            }

            public void Stop()
            {
                navStackService.Stop();
            }

            public void OnViewCreated(IViewFor view)
            {
                Contract.Requires(view != null);

                if (navStack.Head != null)
                {
                    view.ViewModel = navStack.Head;
                }
                else
                {
                    throw new Exception("View created when no model available");
                }
            }
        }
    }
}