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
        Context ApplicationContext { get; }
        void OnCreate();
        void OnTerminate();
    }

    public interface IRxAndroidApplication : IService, IAndroidApplication
    {
        INavigationStackModel<IMobileModel> NavigationStack { get; }

        IMobileApplicationController ApplicationController { get ; }

        void OnActivityCreated(IRxActivity activity);

        Type GetViewType(object model);
    }

    public abstract class RxAndroidApplication : Application, IRxAndroidApplication 
    {
        private readonly INavigationStackModel<IMobileModel> navStack = RxApp.NavigationStack.Create<IMobileModel>();
        private readonly RxAndroidApplicationDelegate deleg;

        public RxAndroidApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            deleg = RxAndroidApplicationDelegate.Create(this);
        }

        public INavigationStackModel<IMobileModel> NavigationStack
        {
            get
            {
                return navStack;
            }
        }

        public abstract IMobileApplicationController ApplicationController { get; }

        public abstract Type GetViewType(object model);
                    
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

    public sealed class RxAndroidApplicationDelegate
    {
        public static RxAndroidApplicationDelegate Create(IRxAndroidApplication application) 
        {
            Contract.Requires(application != null);

            return new RxAndroidApplicationDelegate(application);
        }
            
        private readonly IDictionary<IMobileViewModel, IRxActivity> activities = new Dictionary<IMobileViewModel, IRxActivity> ();

        private readonly IRxAndroidApplication application;
        private readonly IViewHost<RxActivityView> viewHost;

        private IDisposable navStackBinding = null;
        private IDisposable navStackSubscription = null;

        private RxAndroidApplicationDelegate(IRxAndroidApplication application)
        {
            this.application = application;
            this.viewHost = new ApplicationViewHost(this);
        }

        public void OnCreate()
        {
            navStackSubscription = 
                Observable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs<IMobileModel>>(application.NavigationStack, "NavigationStackChanged")
                    .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs<IMobileModel>> e) => 
                        {
                            var newHead = e.EventArgs.NewHead;
                            var oldHead = e.EventArgs.OldHead;

                            if (oldHead != null && newHead == null)
                            {
                                application.Stop();
                            }
                        });

            navStackBinding = 
                application.NavigationStack.Bind<RxActivityView, IMobileModel,IMobileViewModel,IMobileControllerModel> (
                    viewHost,
                    (model) => new RxActivityView(this, model),
                    application.ApplicationController.Bind);
        }

        public void OnTerminate()
        {
            navStackBinding.Dispose();
            navStackSubscription.Dispose();
        }

        public void Start()
        {
            application.ApplicationController.Start();
        }

        public void Stop()
        {
            application.ApplicationController.Stop();
        }

        public void OnActivityCreated(IRxActivity activity)
        {
            Contract.Requires(activity != null);

            if (application.NavigationStack.Head != null)
            {
                activity.ViewModel = application.NavigationStack.Head;
                this.activities[application.NavigationStack.Head] = activity;
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
                var viewType = parent.application.GetViewType(view.ViewModel);
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