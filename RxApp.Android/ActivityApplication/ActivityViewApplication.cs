using Android.App;
using Android.Content;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxApp
{
    public sealed class ActivityViewApplicationDelegate
    {
        public static ActivityViewApplicationDelegate Create(
            IActivityViewApplication deleg,
            Func<IMobileControllerModel, IDisposable> provideController,
            Func<IMobileViewModel, Type> getActivityType) 
        {
            Contract.Requires(deleg != null);
            Contract.Requires(provideController != null);
            Contract.Requires(getActivityType != null);

            return new ActivityViewApplicationDelegate(deleg, provideController, getActivityType);
        }

        private readonly IDictionary<IMobileViewModel, IActivityView> activities = new Dictionary<IMobileViewModel, IActivityView> ();

        private readonly IActivityViewApplication deleg;
        private readonly Func<IMobileControllerModel, IDisposable> provideController;
        private readonly Func<IMobileViewModel, Type> getActivityType;
   
        private CompositeDisposable subscription = null;

        private ActivityViewApplicationDelegate(
            IActivityViewApplication deleg,
            Func<IMobileControllerModel, IDisposable> provideController,
            Func<IMobileViewModel, Type> getActivityType)
        {
            this.deleg = deleg;
            this.provideController = provideController;
            this.getActivityType = getActivityType;
        }

        public void OnCreate()
        {
            subscription = new CompositeDisposable();

            subscription.Add(
                Observable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs<IMobileModel>>(deleg.NavigationStack, "NavigationStackChanged")
                    .Subscribe((EventPattern<NotifyNavigationStackChangedEventArgs<IMobileModel>> e) => 
                        {
                            var newHead = e.EventArgs.NewHead;
                            var oldHead = e.EventArgs.OldHead;
                            var removed = e.EventArgs.Removed;

                            if (oldHead != null && newHead == null)
                            {
                                deleg.Stop();
                            } 
                            else if (newHead != null && !activities.ContainsKey(newHead))
                            {
                                var viewType = getActivityType(newHead);
                                var intent = new Intent(deleg.ApplicationContext, viewType).SetFlags(ActivityFlags.NewTask);
                                deleg.ApplicationContext.StartActivity(intent);
                            }

                            foreach (var model in removed)
                            {
                                IActivityView activity = null;
                                if (activities.TryGetValue(model, out activity))
                                {
                                    activities.Remove(model);
                                    activity.Finish();
                                }
                            }
                    }));

            subscription.Add(deleg.NavigationStack.Bind<IMobileModel, IMobileControllerModel> (provideController));
        }

        public void OnTerminate()
        {
            subscription.Dispose();
        }

        public void OnActivityViewCreated(IActivityView activity)
        {
            Contract.Requires(activity != null);

            var head = deleg.NavigationStack.FirstOrDefault();
            if (head != null)
            {
                activity.ViewModel = head;
                this.activities[head] = activity;
            }
            else
            {
                throw new Exception("View created when no model available");
            }
        }
    }

    public abstract class ActivityViewApplication : Application, IActivityViewApplication
    {
        private readonly INavigationStack<IMobileModel> navStack = RxApp.NavigationStack.Create<IMobileModel>();
        private readonly ActivityViewApplicationDelegate deleg;

        public ActivityViewApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            deleg = ActivityViewApplicationDelegate.Create(this, ProvideController, GetActivityType);
        }

        public INavigationStack<IMobileModel> NavigationStack
        {
            get
            {
                return navStack;
            }
        }

        public abstract Type GetActivityType(IMobileViewModel model);

        public abstract IDisposable ProvideController(IMobileControllerModel model);

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

        public void OnActivityViewCreated(IActivityView activity)
        {
            deleg.OnActivityViewCreated(activity);
        }

        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
        }
    }
}