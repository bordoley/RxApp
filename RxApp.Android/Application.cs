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
    public interface IAndroidApplication 
    {
        void OnCreate();
        void OnTerminate();
    }

    public interface IRxAndroidApplication : IService, IAndroidApplication
    {
        void OnActivityCreated(IRxActivity activity);
    }

    public abstract class RxAndroidApplication : Application, IRxAndroidApplication 
    {
        private readonly INavigationStackModel<IMobileModel> navStack = RxApp.NavigationStack.Create<IMobileModel>();
        private IRxAndroidApplication deleg;

        public RxAndroidApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public INavigationStackModel<IMobileModel> NavigationStack
        {
            get
            {
                return navStack;
            }
        }

        protected abstract Type GetViewType(object model);

        protected abstract IMobileApplicationController ProvideApplicationController();
                    
        public override void OnCreate()
        {
            base.OnCreate();
            deleg = RxAndroidApplicationDelegate.Create(
                this.NavigationStack,
                this,
                this.GetViewType,
                this.ProvideApplicationController());

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
            INavigationStackModel<IMobileModel> navStack,
            Application application, 
            Func<object, Type> viewTypeMap,
            IMobileApplicationController applicationController) 
        {
            Contract.Requires(navStack != null);
            Contract.Requires(application != null);
            Contract.Requires(viewTypeMap != null);
            Contract.Requires(applicationController != null);

            return new RxAndroidApplicationDelegate(navStack, application, viewTypeMap, applicationController);
        }
            
        private readonly IDictionary<IMobileViewModel, IRxActivity> activities = new Dictionary<IMobileViewModel, IRxActivity> ();

        private readonly INavigationStackModel<IMobileModel> navStack;
        private readonly Application application;
        private readonly Func<object, Type> viewTypeMap;
        private readonly IMobileApplicationController applicationController;

        private readonly IViewHost<RxActivityView> viewHost;

        private IDisposable navStackBinding = null;
        private IDisposable navStackSubscription = null;

        private RxAndroidApplicationDelegate(
            INavigationStackModel<IMobileModel> navStack,
            Application application, 
            Func<object, Type> viewTypeMap,
            IMobileApplicationController applicationController)
        {
            this.navStack = navStack;
            this.application = application;
            this.viewTypeMap = viewTypeMap;
            this.applicationController = applicationController;
            this.viewHost = new ApplicationViewHost(this);
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

            navStackBinding = 
                navStack.Bind<RxActivityView, IMobileModel,IMobileViewModel,IMobileControllerModel> (
                    viewHost,
                    (model) => new RxActivityView(this, model),
                    applicationController.Bind);
        }

        public void OnTerminate()
        {
            navStackBinding.Dispose();
            navStackSubscription.Dispose();
        }

        public void Start()
        {
            applicationController.Start();
        }

        public void Stop()
        {
            applicationController.Stop();
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

        private sealed class ApplicationViewHost : IViewHost<RxActivityView>
        {
            private readonly RxAndroidApplicationDelegate parent;

            internal ApplicationViewHost(RxAndroidApplicationDelegate parent)
            {
                this.parent = parent;
            }

            public void PresentView(RxActivityView view)
            {
                var viewType = parent.viewTypeMap(view.ViewModel);
                var intent = new Intent(parent.application.ApplicationContext, viewType).SetFlags(ActivityFlags.NewTask);
                parent.application.ApplicationContext.StartActivity(intent);
            }
        }

        private sealed class RxActivityView : IDisposable
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