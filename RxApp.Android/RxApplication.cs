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

using RxObservable = System.Reactive.Linq.Observable;
using System.Reflection;

namespace RxApp.Android
{
    public interface IRxApplication
    {
        void OnActivityCreated<TActivity>(TActivity activity) where TActivity: Activity, IViewFor;
    }

    public sealed class RxApplicationHelper
    {
        public static RxApplicationHelper Create(
            Func<IObservable<NavigationStack>> navigationApplicaction,
            Action<Activity,INavigationViewModel> createActivity) 
        {
            Contract.Requires(navigationApplicaction != null);
            Contract.Requires(createActivity != null);

            return new RxApplicationHelper(navigationApplicaction, createActivity);
        }

        private readonly Func<IObservable<NavigationStack>> navigationApplicaction;

        private readonly Action<Activity,INavigationViewModel> createActivity;

        private readonly Subject<IViewFor> activityCreated = new Subject<IViewFor>();

        private IDisposable subscription;

        private RxApplicationHelper(
            Func<IObservable<NavigationStack>> navigationApplicaction,
            Action<Activity,INavigationViewModel> createActivity)
        {
            this.navigationApplicaction = navigationApplicaction;
            this.createActivity = createActivity;
        }

        public void OnActivityCreated<TActivity>(TActivity activity) 
            where TActivity: Activity, IViewFor
        {
            Contract.Requires(activity != null);

            if (subscription == null)
            {
                // Either the startup activity called OnActivityCreated or the application was killed and restarted by android.
                // If the application is backgrounded, android will kill all the activities and the application class.
                // When the application is reopened from the background, it creates the application and starts the last activity 
                // that was opened, not the startup activity. 

                this.Start(activity);
            }
            else
            {
                this.activityCreated.OnNext(activity);
            }
        }

        private void Start<TActivity>(TActivity startupActivity) 
            where TActivity: Activity, IViewFor
        {
            Log.Debug("RxApp", "Starting application");

            var activities = new Dictionary<INavigationViewModel, IViewFor> ();
            INavigationViewModel currentModel = new StartupModel();
            activities[currentModel] = startupActivity;

            // This is essentially an async lock. This code is single threaded so using a bool is ok.
            var canCreateActivity = RxProperty.Create(true);
      
            subscription = Disposable.Compose(
                this.navigationApplicaction()
                    .ObserveOnMainThread()
                    .Delay(_ => canCreateActivity.Where(b => b))
                    .Scan(NavigationStack.Empty, (previous, navStack) =>
                        {
                            var removed = activities.Keys.Where(y => !navStack.Contains(y)).ToImmutableArray();
                            var previousActivity = activities[currentModel];
                            currentModel = navStack.FirstOrDefault();

                            if (currentModel != null && !activities.ContainsKey(currentModel))
                            {
                                Log.Debug("RxApp", "Starting activity with model: " + currentModel.GetType());

                                canCreateActivity.Value = false;
                                createActivity((Activity) previousActivity, currentModel);
                            }

                            if ((navStack.IsEmpty || navStack.Pop().Equals(previous)) && ((int) Build.VERSION.SdkInt >= 21))
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

                            if (navStack.IsEmpty)
                            {
                                // Can't dispose the outer subscription from within its own callback, 
                                // so post the call onto the sync context
                                SynchronizationContext.Current.Post(_ => 
                                    {
                                        subscription.Dispose(); 
                                        subscription = null;
                                    }, null);
                            } 

                            return navStack;
                        })
                    .Subscribe(),

                this.activityCreated
                    .Subscribe(activity => 
                        {
                            Log.Debug("RxApp", "Activity created of type: " + activity.GetType() + ", with model of type: " + currentModel.GetType());

                            activity.ViewModel = currentModel;
                            activities[currentModel] = activity;
                            canCreateActivity.Value = true;
                        })
            );
        }

        private sealed class StartupModel : NavigationModel
        {
        }
    }

    public abstract class RxApplication : Application, IRxApplication
    {
        private readonly Dictionary<Type, Type> modelToActivityMapping = new Dictionary<Type, Type>();
        private RxApplicationHelper helper;

        public RxApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            helper = RxApplicationHelper.Create(() => this.NavigationApplicaction, this.CreateActivity);
        }

        protected abstract IObservable<NavigationStack> NavigationApplicaction { get; }

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

        private void CreateActivity(Activity current, INavigationViewModel model)
        {
            var viewType = GetActivityType(model);
            StartActivity(current, viewType);
        }

        // This method can be overrided to implement custom activity transitions.
        protected virtual void StartActivity(Activity current, Type type)
        {
            var intent = new Intent(current, type);
            current.StartActivity(intent);
        }

        protected void RegisterActivity<TModel, TActivity>()
            where TModel : INavigationViewModel
            where TActivity : Activity, IViewFor
        {
            this.modelToActivityMapping.Add(typeof(TModel), typeof(TActivity));
        }

        public void OnActivityCreated<TActivity>(TActivity activity) 
            where TActivity: Activity, IViewFor
        {
            helper.OnActivityCreated(activity);
        }
    }
}