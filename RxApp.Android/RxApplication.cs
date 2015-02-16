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
            Context context,
            Func<object,Type> getActivityType,
            Func<INavigationStack,IApplication> provideApplication) 
        {
            Contract.Requires(context != null);
            Contract.Requires(getActivityType != null);
            Contract.Requires(provideApplication != null);

            return new RxApplicationHelper(context, getActivityType, provideApplication);
        }


        private readonly IDictionary<object, IRxActivity> activities = new Dictionary<object, IRxActivity> ();

        private readonly Context context;

        private readonly Func<object,Type> getActivityType;

        private readonly Func<INavigationStack,IApplication> provideApplication;

        private readonly INavigationStack navStack = RxApp.NavigationStack.Create();
   

        private CompositeDisposable subscription;

        private IApplication application;


        private RxApplicationHelper(
            Context context,
            Func<object,Type> getActivityType,
            Func<INavigationStack,IApplication> provideApplication)
        {
            this.context = context;
            this.getActivityType = getActivityType;
            this.provideApplication = provideApplication;
        }

        public void OnCreate()
        {
            subscription = new CompositeDisposable();

            subscription.Add(
                Observable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs>(navStack, "NavigationStackChanged")
                    .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs> e) => 
                        {
                            var newHead = e.EventArgs.NewHead;
                            var oldHead = e.EventArgs.OldHead;
                            var removed = e.EventArgs.Removed;

                            if (oldHead != null && newHead == null)
                            {
                                this.Stop();
                            } 
                            else if (newHead != null && !activities.ContainsKey(newHead))
                            {
                                var viewType = getActivityType(newHead);
                                var intent = new Intent(context, viewType).SetFlags(ActivityFlags.NewTask);
                                context.StartActivity(intent);
                            }

                            foreach (var model in removed)
                            {
                                IRxActivity activity = activities[model];
                                activities.Remove(model);
                                activity.Finish();
                            }
                    }));

            subscription.Add(navStack.BindController(model => application.Bind(model)));
        }

        public void OnTerminate()
        {
            subscription.Dispose();
        }

        public void OnActivityCreated(IRxActivity activity)
        {
            Contract.Requires(activity != null);

            var head = navStack.FirstOrDefault();
            if (head != null)
            {
                activity.ViewModel = head;
                this.activities[head] = activity;
            }
            else
            {
                // Either the startup activity called OnActivityCreated or the application was killed and restarted by android.
                // If the application is backgrounded, android will kill all the activities and the application class.
                // When the application is reopened from the background, it creates the application and starts the last activity 
                // that was opened, not the startup activity. 

                this.Start();
                activity.Finish();

                Log.Debug("RxApplicationHelper", "Activity of type " + activity.GetType() + " created when the navigation stack was empty."); 
            }
        }

        private void Start()
        {
            application = provideApplication(navStack);
            application.Init();
        }

        private void Stop()
        {
            Log.Debug("RxApplicationHelper", "RxApplication.Stop()"); 
            application.Dispose(); 
        }
    }

    public abstract class RxApplication : Application, IRxApplication
    {
        private readonly RxApplicationHelper helper;

        public RxApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            helper = RxApplicationHelper.Create(this.ApplicationContext, this.GetActivityType, this.ProvideApplication);
        }

        public abstract Type GetActivityType(object model);

        public abstract IApplication ProvideApplication(INavigationStack navStack);

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
    }
}