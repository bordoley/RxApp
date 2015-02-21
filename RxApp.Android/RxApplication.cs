using Android.App;
using Android.Content;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Android.Content.PM;
using Android.Runtime;

using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp.Android
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

        private readonly INavigationStack navStack = NavigationStack.Create(Observable.MainThreadScheduler);

        private readonly Context context;

        private readonly Func<object,Type> getActivityType;

        private readonly Func<INavigationStack,IApplication> provideApplication;


        private IDisposable subscription;

        private RxApplicationHelper(
            Context context,
            Func<object,Type> getActivityType,
            Func<INavigationStack,IApplication> provideApplication)
        {
            this.context = context;
            this.getActivityType = getActivityType;
            this.provideApplication = provideApplication;
        }

        public void OnTerminate()
        {
            this.Stop();
        }

        public void OnActivityCreated(IRxActivity activity)
        {
            Contract.Requires(activity != null);

            var head = navStack.FirstOrDefault();
            if (head != null)
            {
                try
                {
                    activity.ViewModel = head;
                }
                catch (InvalidCastException e)
                {
                    var activityType = activity.GetType().ToString();
                    var modelType = activity.GetType().ToString();

                    throw new InvalidOperationException("Current model is of type: " + modelType + " which can not be bound to an Activity of type: " + activityType);
                }
                this.activities[head] = activity;
            }
            else if (subscription == null)
            {
                // Either the startup activity called OnActivityCreated or the application was killed and restarted by android.
                // If the application is backgrounded, android will kill all the activities and the application class.
                // When the application is reopened from the background, it creates the application and starts the last activity 
                // that was opened, not the startup activity. 

                this.Start();
                activity.Finish();
            }
            else
            {
                throw new InvalidOperationException( 
                    "Activity of type " + activity.GetType() + " created when the navigation stack head was null, but the application was running. Something is badly broken.");
            }
        }

        private void Start()
        {
            var application = provideApplication(navStack);

            var subscription = new CompositeDisposable();
            subscription.Add(application);
            subscription.Add(
                RxObservable.FromEventPattern<NotifyNavigationStackChangedEventArgs>(navStack, "NavigationStackChanged")

                    // The Stack can be updated from multiple threads
                    .ObserveOnMainThread()
                    .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs> e) => 
                        {
                            var newHead = e.EventArgs.NewHead;
                            var oldHead = e.EventArgs.OldHead;
                            var removed = e.EventArgs.Removed;

                            if (oldHead != null && newHead == null)
                            {
                                // Post the call to stop on the event loop to avoid deadlocking.
                                SynchronizationContext.Current.Post(_ => this.Stop(), null);
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

            this.subscription = subscription;
        
            application.Init();
        }

        private void Stop()
        {
            subscription.Dispose(); 
            subscription = null;
        }
    }

    public abstract class RxApplication : Application, IRxApplication
    {
        private readonly RxApplicationHelper helper;

        public RxApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            helper = RxApplicationHelper.Create(this.ApplicationContext, this.GetActivityType, this.ProvideApplication);
        }

        public abstract Type GetActivityType(object model);

        public abstract IApplication ProvideApplication(INavigationStack navStack);

        public override void OnTerminate()
        {
            helper.OnTerminate();
            base.OnTerminate();
        }

        public void OnActivityCreated(IRxActivity activity)
        {
            helper.OnActivityCreated(activity);
        }
    }
}