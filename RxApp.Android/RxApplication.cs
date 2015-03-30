﻿using Android.App;
using Android.Content;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using System.Reflection;

namespace RxApp.Android
{
    public sealed class RxApplicationHelper
    {
        public static RxApplicationHelper Create(
            Context context,
            IConnectableObservable<ImmutableStack<INavigationModel>> navigationApplicaction,
            Func<INavigationViewModel,Type> getActivityType) 
        {
            Contract.Requires(context != null);
            Contract.Requires(navigationApplicaction != null);
            Contract.Requires(getActivityType != null);

            return new RxApplicationHelper(context, navigationApplicaction, getActivityType);
        }

        private readonly Context context;

        private readonly IConnectableObservable<ImmutableStack<INavigationModel>> navigationApplicaction;

        private readonly Func<INavigationViewModel,Type> getActivityType;

        private readonly Subject<IRxActivity> activityCreated = new Subject<IRxActivity>();

        private IDisposable subscription;

        private RxApplicationHelper(
            Context context,
            IConnectableObservable<ImmutableStack<INavigationModel>> navigationApplicaction,
            Func<INavigationViewModel,Type> getActivityType)
        {
            this.context = context;
            this.navigationApplicaction = navigationApplicaction;
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
            Log.Debug("RxApp", "Starting application: " + this.context.ApplicationInfo.ClassName);

            var activities = new Dictionary<INavigationViewModel, IRxActivity> ();

            Action<IEnumerable<INavigationViewModel>> finishRemovedActivities = removed =>
                {
                    foreach (var model in removed)
                    {
                        IRxActivity activity = activities[model];  
                        activities.Remove(model);
                        activity.Finish();
                    }   
                };

            // This is essentially an async lock. This code is single threaded so using a bool is ok.
            var canCreateActivity = RxProperty.Create(true);
            INavigationViewModel currentModel = null;


            // FIXME: Need to support activity transitions.
            Action<INavigationViewModel> createActivity = model =>
                {
                    var viewType = getActivityType(model);
                    var intent = new Intent(context, viewType).AddFlags(ActivityFlags.NewTask);

                    canCreateActivity.Value = false;
                    currentModel = model;
                    context.StartActivity(intent);
                };
                    
            subscription = Disposable.Compose(
                this.navigationApplicaction
                    .ObserveOnMainThread()
                    .Scan(Tuple.Create(ImmutableStack<INavigationModel>.Empty, ImmutableStack<INavigationModel>.Empty), (acc, next) =>
                        Tuple.Create(acc.Item2, next))
                    .Delay(x => canCreateActivity.Where(b => b))
                    .Do(x =>
                        {
                            var newHead = x.Item2.IsEmpty ? null :x.Item2.Peek();
                            var oldHead = x.Item1.IsEmpty ? null : x.Item1.Peek();

                            var newHeadSet = x.Item2.ToImmutableHashSet();
                            var removed = x.Item1.Where(y => !newHeadSet.Contains(y));

                            if (newHead == null)
                            {
                                // Back button clicked clearing the model stack
                                currentModel = null;
                                finishRemovedActivities(removed);

                                // Can't dispose the outer subscription from within its own callback, 
                                // so post the call onto the sync context
                                SynchronizationContext.Current.Post(_ => this.Stop(), null);
                            } 
                            else if (activities.ContainsKey(newHead))
                            {
                                // Back button clicked to a previous model in the stack.
                                // Android still maintains the visual stack and will display
                                // the correct view once we close all the other activities 
                                // that have been popped from the model stack.
                                currentModel = newHead;
                                finishRemovedActivities(removed); 
                            }
                            else if (oldHead == null)
                            {
                                // Special case application start up since, we want to start the first application
                                // activity immediately to avoid any weird visual glitches when transitioning from
                                // the splash screen to the first activity of the app (which is sometimes an empty activity).
                                Log.Debug("RxApp", "Starting activity with model: " + newHead.GetType());

                                createActivity(newHead);
                            }
                            else
                            {
                                // Force the action to be placed on the event loop in order to ensure that each started activity
                                // resumes with the correct model as current model.
                                SynchronizationContext.Current.Post(_ =>
                                    {
                                        Log.Debug("RxApp", "Starting activity with model: " + newHead.GetType());

                                        createActivity(newHead);
                                        finishRemovedActivities(removed);   
                                    }, null);
                            }
                        }).Subscribe(),

                this.activityCreated
                    .Subscribe(activity => 
                        {
                            Log.Debug("RxApp", "Activity created of type: " + activity.GetType() + ", with model of type: " + currentModel.GetType());

                            activity.ViewModel = currentModel;
                            activities[currentModel] = activity;
                            canCreateActivity.Value = true;
                        }),

                this.navigationApplicaction.Connect()   
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
        private readonly Dictionary<Type, Type> modelToActivityMapping = new Dictionary<Type, Type>();
        private RxApplicationHelper helper;

        public RxApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected abstract IConnectableObservable<ImmutableStack<INavigationModel>> NavigationApplicaction { get; }

        private Type GetActivityType(INavigationViewModel model)
        {
            var modelType = model.GetType();

            foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
            {
                Type activityType;
                if (this.modelToActivityMapping.TryGetValue(iface, out activityType))
                {
                    return activityType;
                }
            }

            throw new NotSupportedException("No activity found for the given model type: " + modelType);
        }

        protected void RegisterActivity<TModel, TActivity>()
            where TModel : INavigationViewModel
            where TActivity : Activity, IRxActivity
        {
            this.modelToActivityMapping.Add(typeof(TModel), typeof(TActivity));
        }

        public override void OnCreate()
        {   
            base.OnCreate();
            helper = RxApplicationHelper.Create(this.ApplicationContext, this.NavigationApplicaction, this.GetActivityType);
        }

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