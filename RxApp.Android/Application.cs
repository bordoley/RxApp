using Android.App;
using Android.Content;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace RxApp
{
    public interface IRxAndroidApplication : IService
    {
        INavigationStackModel<IMobileModel> NavigationStack { get; }
        void OnCreate();
        void OnTerminate();
        void OnActivityCreated(IRxActivity activity);
    }

    public abstract class RxAndroidApplication : Application, IRxAndroidApplication 
    {
        private readonly IRxAndroidApplication deleg;

        public RxAndroidApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            deleg = RxAndroidApplicationDelegate.Create(
                this,
                this.GetViewType,
                this.ProvideControllerBinder);
        }

        public INavigationStackModel<IMobileModel> NavigationStack
        {
            get
            {
                return deleg.NavigationStack;
            }
        }

        protected abstract Type GetViewType(object model);

        protected abstract IControllerModelBinder<IMobileControllerModel> ProvideControllerBinder();
                    
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

        public void OnActivityCreated(IRxActivity activity)
        {
            deleg.OnActivityCreated(activity);
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

    public sealed class RxAndroidApplicationDelegate : IRxAndroidApplication
    {
        public static RxAndroidApplicationDelegate Create(
            Application application, 
            Func<object, Type> viewTypeMap,
            Func<IControllerModelBinder<IMobileControllerModel>> controllerModelBinderProvider)
        {
            Contract.Requires(application != null);
            Contract.Requires(viewTypeMap != null);
            Contract.Requires(controllerModelBinderProvider != null);

            return new RxAndroidApplicationDelegate(application, viewTypeMap, controllerModelBinderProvider);
        }

        private readonly INavigationStackModel<IMobileModel> navStack = RxApp.NavigationStack.Create<IMobileModel>();
        private readonly IDictionary<IMobileViewModel, IRxActivity> activities = new Dictionary<IMobileViewModel, IRxActivity> ();

        private readonly Application application;
        private readonly Func<object, Type> viewTypeMap;
        private readonly Func<IControllerModelBinder<IMobileControllerModel>> controllerModelBinderProvider;

        private readonly IViewHost viewHost;

        private IDisposableService navStackService = null;
        private IDisposable navStackSubscription = null;

        private RxAndroidApplicationDelegate(
            Application application, 
            Func<object, Type> viewTypeMap,
            Func<IControllerModelBinder<IMobileControllerModel>> controllerModelBinderProvider)
        {
            this.application = application;
            this.viewTypeMap = viewTypeMap;
            this.controllerModelBinderProvider = controllerModelBinderProvider;
            this.viewHost = new ApplicationViewHost(this);
        }

        public INavigationStackModel<IMobileModel> NavigationStack 
        { 
            get
            { 
                return navStack;
            }
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
            navStackService = 
                navStack.Bind<IMobileModel,IMobileViewModel,IMobileControllerModel> (
                    viewHost,
                    () => new ApplicationViewModelBinder(this),
                    controllerModelBinderProvider);
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

        public void OnActivityCreated(IRxActivity activity)
        {
            Contract.Requires(activity != null);

            if (navStack.Head != null)
            {
                activity.ViewModel = navStack.Head;
                this.activities[navStack.Head] = activity;
            }
            else
            {
                throw new Exception("View created when no model available");
            }
        }

        private class ApplicationViewHost : IViewHost
        {
            private readonly RxAndroidApplicationDelegate parent;

            internal ApplicationViewHost(RxAndroidApplicationDelegate parent)
            {
                this.parent = parent;
            }

            public void PresentView(IViewFor view)
            {
                var viewType = parent.viewTypeMap(view.ViewModel);
                var intent = new Intent(parent.application.ApplicationContext, viewType).SetFlags(ActivityFlags.NewTask);
                parent.application.ApplicationContext.StartActivity(intent);
            }
        }

        private sealed class ApplicationViewModelBinder : IViewModelBinder<IMobileViewModel>
        {
            private readonly RxAndroidApplicationDelegate parent;

            internal ApplicationViewModelBinder(RxAndroidApplicationDelegate parent)
            {
                this.parent = parent;
            }

            public IView Bind(IMobileViewModel model)
            {
                return new RxActivityView(parent, model);
            }

            public void Initialize()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class RxActivityView : IView
        {
            private readonly RxAndroidApplicationDelegate parent;
            private readonly IMobileViewModel viewModel;

            internal RxActivityView(RxAndroidApplicationDelegate parent, IMobileViewModel viewModel)
            {
                this.parent = parent;
                this.viewModel = viewModel;
            }

            public void Dispose()
            {
                IRxActivity activity = null;
                if (parent.activities.TryGetValue(viewModel, out activity))
                {
                    parent.activities.Remove(viewModel);
                    activity.Finish();
                }
            }

            public object ViewModel
            {
                get
                {
                    return viewModel;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}