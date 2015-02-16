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
            IRxApplication androidApplication,
            Func<IApplication> provideApplication) 
        {
            Contract.Requires(androidApplication != null);
            Contract.Requires(provideApplication != null);

            return new RxApplicationHelper(androidApplication, provideApplication);
        }


        private readonly IDictionary<object, IRxActivity> activities = new Dictionary<object, IRxActivity> ();

        private readonly IRxApplication androidApplication;

        private readonly Func<IApplication> provideApplication;
   

        private CompositeDisposable subscription;

        private IApplication application;


        private RxApplicationHelper(
            IRxApplication androidApplication,
            Func<IApplication> provideApplication)
        {
            this.androidApplication = androidApplication;
            this.provideApplication = provideApplication;
        }

        public void OnCreate()
        {
            subscription = new CompositeDisposable();

            subscription.Add(
                Observable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs>(androidApplication.NavigationStack, "NavigationStackChanged")
                    .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs> e) => 
                        {
                            var newHead = e.EventArgs.NewHead;
                            var oldHead = e.EventArgs.OldHead;
                            var removed = e.EventArgs.Removed;

                            if (oldHead != null && newHead == null)
                            {
                                androidApplication.Stop();
                            } 
                            else if (newHead != null && !activities.ContainsKey(newHead))
                            {
                                var viewType = androidApplication.GetActivityType(newHead);
                                var intent = new Intent(androidApplication.ApplicationContext, viewType).SetFlags(ActivityFlags.NewTask);
                                androidApplication.ApplicationContext.StartActivity(intent);
                            }

                            foreach (var model in removed)
                            {
                                IRxActivity activity = activities[model];
                                activities.Remove(model);
                                activity.Finish();
                            }
                    }));

            subscription.Add(androidApplication.NavigationStack.BindController(model => application.Bind(model)));
        }

        public void OnTerminate()
        {
            subscription.Dispose();
        }

        public void OnActivityCreated(IRxActivity activity)
        {
            Contract.Requires(activity != null);

            var head = androidApplication.NavigationStack.FirstOrDefault();
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
                androidApplication.Start();
                activity.Finish();

                Log.Debug("RxApplicationHelper", "Activity of type " + activity.GetType() + " created when the navigation stack was empty."); 
            }
        }

        public void Start()
        {
            application = provideApplication();
            application.Init();
        }

        public void Stop()
        {
            Log.Debug("RxApplicationHelper", "RxApplication.Stop()"); 
            application.Dispose(); 
        }
    }

    public abstract class RxApplication : Application, IRxApplication
    {
        private readonly INavigationStack navStack = RxApp.NavigationStack.Create();
        private readonly RxApplicationHelper helper;

        public RxApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            helper = RxApplicationHelper.Create(this, ProvideApplication);
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
            helper.Start();
        }

        public void Stop()
        {
            helper.Stop();
        }
    }
}