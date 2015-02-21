using Android.App;
using Android.Content;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
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
            IApplication application,
            Func<object,Type> getActivityType) 
        {
            Contract.Requires(context != null);
            Contract.Requires(getActivityType != null);
            Contract.Requires(application != null);

            return new RxApplicationHelper(context, application, getActivityType);
        }

        private readonly IDictionary<object, IRxActivity> activities = new Dictionary<object, IRxActivity> ();

        private readonly INavigationStack navStack = NavigationStack.Create(Observable.MainThreadScheduler);

        private readonly Context context;

        private readonly IApplication application;

        private readonly Func<object,Type> getActivityType;

        private readonly Subject<IRxActivity> activityCreated = new Subject<IRxActivity>();

        private IDisposable subscription;

        private RxApplicationHelper(
            Context context,
            IApplication application,
            Func<object,Type> getActivityType)
        {
            this.context = context;
            this.application = application;
            this.getActivityType = getActivityType;
        }

        public void OnTerminate()
        {
            this.Stop();
        }

        public void OnActivityCreated(IRxActivity activity)
        {
            Contract.Requires(activity != null);

            if (subscription == null)
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
                this.activityCreated.OnNext(activity);
            }
        }

        private void Start()
        {
            var navStackChanged = 
                RxObservable.FromEventPattern<NotifyNavigationStackChangedEventArgs>(navStack, "NavigationStackChanged");

            subscription = Disposable.Combine(
                navStackChanged
                    .Do(e =>
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
                        })
                    .Select(x => x.EventArgs.NewHead)
                    .Where(x => x != null)
                    .SelectMany(x => 
                        this.activityCreated
                            .Select(y => Tuple.Create(x, y))
                            .TakeUntil(navStackChanged))
                    .Subscribe(x => 
                        {
                            var activity = x.Item2;
                            var model = x.Item1;

                            activity.ViewModel = model;
                            activities[model] = activity;
                        }),

                navStack.BindController(model => application.Bind(model)),
                    
                application.ResetApplicationState.ObserveOnMainThread().Subscribe(x => 
                    navStack.SetRoot(x))
            );
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
            helper = RxApplicationHelper.Create(this.ApplicationContext, this.ProvideApplication(), this.GetActivityType);
        }

        public abstract Type GetActivityType(object model);

        public abstract IApplication ProvideApplication();

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