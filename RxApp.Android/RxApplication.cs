using Android.App;
using Android.Content;
using Android.Util;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Content.PM;

namespace RxApp
{
    public sealed class RxApplicationHelper
    {
        public static RxApplicationHelper Create(
            IRxApplication application,
            Func<object, IDisposable> provideController,
            Func<object, Type> getActivityType) 
        {
            Contract.Requires(application != null);
            Contract.Requires(provideController != null);
            Contract.Requires(getActivityType != null);

            return new RxApplicationHelper(application, provideController, getActivityType);
        }

        private readonly IDictionary<object, IRxActivity> activities = new Dictionary<object, IRxActivity> ();

        private readonly IRxApplication application;
        private readonly Func<object, IDisposable> provideController;
        private readonly Func<object, Type> getActivityType;
   
        private CompositeDisposable subscription = null;

        private RxApplicationHelper(
            IRxApplication application,
            Func<object, IDisposable> provideController,
            Func<object, Type> getActivityType)
        {
            this.application = application;
            this.provideController = provideController;
            this.getActivityType = getActivityType;
        }

        public void OnCreate()
        {
            subscription = new CompositeDisposable();

            subscription.Add(
                Observable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs>(application.NavigationStack, "NavigationStackChanged")
                    .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs> e) => 
                        {
                            var newHead = e.EventArgs.NewHead;
                            var oldHead = e.EventArgs.OldHead;
                            var removed = e.EventArgs.Removed;

                            if (oldHead != null && newHead == null)
                            {
                                application.Stop();
                            } 
                            else if (newHead != null && !activities.ContainsKey(newHead))
                            {
                                var viewType = getActivityType(newHead);
                                var intent = new Intent(application.ApplicationContext, viewType).SetFlags(ActivityFlags.NewTask);
                                application.ApplicationContext.StartActivity(intent);
                            }

                            foreach (var model in removed)
                            {
                                IRxActivity activity = activities[model];
                                activities.Remove(model);
                                activity.Finish();
                            }
                    }));

            subscription.Add(application.NavigationStack.BindController(provideController));
        }

        public void OnTerminate()
        {
            subscription.Dispose();
        }

        public void OnActivityCreated(IRxActivity activity)
        {
            Contract.Requires(activity != null);

            var head = application.NavigationStack.FirstOrDefault();
            if (head != null)
            {
                activity.ViewModel = head;
                this.activities[head] = activity;
            }
            else
            {
                // If the application is backgrounded, android will kill all the activities and the application class.
                // When the application is reopened from the background, it creates the application and starts the last activity 
                // that was opened, not the startup activity. So instead, we start the application and finish the activity that was 
                // started by android.
                application.Start();
                activity.Finish();

                Log.Debug("RxApplicationHelper", "Activity of type " + activity.GetType() + " created when the navigation stack was empty."); 
            }
        }
    }

    public abstract class RxApplication : Application, IRxApplication
    {
        private readonly INavigationStack navStack = RxApp.NavigationStack.Create();
        private readonly RxApplicationHelper helper;

        private IApplication application;

        public RxApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            helper = RxApplicationHelper.Create(this, model => application.Bind(model), GetActivityType);
        }

        public INavigationStack NavigationStack
        {
            get
            {
                return navStack;
            }
        }

        public abstract Type GetActivityType(object model);

        public abstract IApplication ProvideApplication();

        public override void OnCreate()
        {
            base.OnCreate();

            Log.Debug("RxApplication", "RxApplication.OnCreate()");
            helper.OnCreate();
        }

        public override void OnTerminate()
        {
            helper.OnTerminate();
            Log.Debug("RxApplication", "RxApplication.OnTerminate()");

            base.OnTerminate();
        }

        public void OnActivityCreated(IRxActivity activity)
        {
            helper.OnActivityCreated(activity);
        }

        public void Start()
        {
            application = ProvideApplication();
            application.Init();
        }

        public void Stop()
        {
            Log.Debug("RxApplication", "RxApplication.Stop()"); 
            application.Dispose(); 
        }
    }
}