using Android.App;
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
using Android.OS;

using AndroidBuild = Android.OS.Build;
using RxObservable = System.Reactive.Linq.Observable;
using System.Reflection;

namespace RxApp.Android
{
    public sealed class RxAndroidApplicationBuilder
    {
        private static Type GetActivityType(IReadOnlyDictionary<Type, Type> activityMapping, INavigationViewModel model)
        {
            var modelType = model.GetType();

            foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
            {
                Type activityType;
                if (activityMapping.TryGetValue(iface, out activityType))
                {
                    return activityType;
                }
            }

            throw new NotSupportedException("No activity found for the given model type: " + modelType);
        }

        private readonly Dictionary<Type, Type> activityMapping = new Dictionary<Type, Type>();

        public IObservable<NavigationStack> NavigationApplicaction { get; set; }

        public Action<Activity,Type> StartActivity { get; set; }

        public IObservable<IViewFor> CreatedActivities { get; set; }

        public void RegisterActivityMapping<TModel, TActivity>()
            where TModel : INavigationViewModel
            where TActivity : Activity, IViewFor
        {
            this.activityMapping.Add(typeof(TModel), typeof(TActivity));
        }

        public IObservable<NavigationStack> Build()
        {
            var activityMapping = this.activityMapping.ToImmutableDictionary();

            if (this.NavigationApplicaction == null) { throw new NotSupportedException("Application must not be null"); }
            var navigationApplication = this.NavigationApplicaction;

            if (this.CreatedActivities == null) { throw new NotSupportedException("Activity stream must not be null"); }
            var createdActivities = this.CreatedActivities;

            var startActivity = this.StartActivity ?? ((current, type) => 
                {
                    var intent = new Intent(current, type);
                    current.StartActivity(intent);
                });

            return RxObservable.Create<NavigationStack>(observer =>
                {
                    var activities = new Dictionary<INavigationViewModel, IViewFor> ();

                    // This is essentially an async lock. This code is single threaded so using a bool is ok.
                    var canCreateActivity = RxProperty.Create(true);
                    INavigationViewModel currentModel = null;

                    var appBinding = navigationApplication
                        .ObserveOnMainThread()
                        .Delay(_ => canCreateActivity.Where(b => b))
                        .Scan(NavigationStack.Empty, (previous, navStack) =>
                            {
                                var removed = activities.Keys.Where(y => !navStack.Contains(y)).ToImmutableArray();
                                var previousActivity = activities[currentModel];
                                currentModel = navStack.FirstOrDefault();

                                if (currentModel != null && !activities.ContainsKey(currentModel))
                                {
                                    canCreateActivity.Value = false;
                                    var viewType = GetActivityType(activityMapping, currentModel);
                                    startActivity((Activity) previousActivity, viewType);
                                }

                                if (((int) AndroidBuild.VERSION.SdkInt >= 21) && !previous.IsEmpty && previous.Pop().Equals(navStack))
                                {
                                    // Show the inverse activity transition after the back button is clicked.
                                    ((Activity) previousActivity).FinishAfterTransition();
                                }
                                else
                                {
                                    foreach (var model in removed)
                                    {
                                        IViewFor activity = activities[model];  
                                        activities.Remove(model);
                                        ((Activity) activity).Finish();
                                    }   
                                }

                                return navStack;
                            }).Do(observer);

                    IDisposable subscription = null;

                    return createdActivities.Subscribe(activity => 
                        {
                            if (subscription == null)
                            {
                                // Either the startup activity called OnActivityCreated or the application was killed and restarted by android.
                                // If the application is backgrounded, android will kill all the activities and the application class.
                                // When the application is reopened from the background, it creates the application and starts the last activity 
                                // that was opened, not the startup activity. 
                                currentModel = new StartupModel();
                                activities[currentModel] = activity;

                                subscription = appBinding.Where(x => x.IsEmpty).Subscribe(x => 
                                    {
                                        // Can't dispose the outer subscription from within its own callback, 
                                        // so post the call onto the sync context
                                        SynchronizationContext.Current.Post(_ => 
                                            {
                                                subscription.Dispose(); 
                                                subscription = null;
                                            }, null);
                                    });
                            }
                            else
                            {
                                activity.ViewModel = currentModel;
                                activities[currentModel] = activity;
                                canCreateActivity.Value = true;
                            }
                        });
                });
        }  

        private sealed class StartupModel : NavigationModel
        {
        }
    }

    public interface IRxApplication
    {
        void OnActivityCreated<TActivity>(TActivity activity) where TActivity: Activity, IViewFor;
    }

    public abstract class RxApplication : Application, IRxApplication
    {
        private readonly Subject<IViewFor> createdActivities = new Subject<IViewFor>();

        public RxApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public IObservable<IViewFor> CreatedActivities { get { return createdActivities; } }

        public void OnActivityCreated<TActivity>(TActivity activity) 
            where TActivity: Activity, IViewFor
        {
            createdActivities.OnNext(activity);
        }
    }
}