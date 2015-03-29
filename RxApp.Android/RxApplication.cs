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
using System.Reflection;

namespace RxApp.Android
{
    public sealed class RxApplicationHelper
    {
        public static RxApplicationHelper Create(
            Context context,
            Func<IObservable<IEnumerable<INavigationModel>>> getApplication,
            Func<INavigationViewModel,Type> getActivityType) 
        {
            Contract.Requires(context != null);
            Contract.Requires(getApplication != null);
            Contract.Requires(getActivityType != null);

            return new RxApplicationHelper(context, getApplication, getActivityType);
        }

        private readonly Context context;

        private readonly Func<IObservable<IEnumerable<INavigationModel>>> getApplication;

        private readonly Func<INavigationViewModel,Type> getActivityType;

        private readonly Subject<IRxActivity> activityCreated = new Subject<IRxActivity>();

        private IDisposable subscription;

        private RxApplicationHelper(
            Context context,
            Func<IObservable<IEnumerable<INavigationModel>>> getApplication,
            Func<INavigationViewModel,Type> getActivityType)
        {
            this.context = context;
            this.getApplication = getApplication;
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

            var application = getApplication();

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

            Action<INavigationViewModel> createActivity = model =>
                {
                    var viewType = getActivityType(model);
                    var intent = new Intent(context, viewType).SetFlags(ActivityFlags.NewTask);

                    canCreateActivity.Value = false;
                    currentModel = model;
                    context.StartActivity(intent);
                };
                    
            subscription = Disposable.Compose(
                application
                    .ObserveOnMainThread()
                    .Scan(Tuple.Create<IEnumerable<INavigationModel>, IEnumerable<INavigationModel>>(Enumerable.Empty<INavigationModel>(), Enumerable.Empty<INavigationModel>()), (acc, next) =>
                        {
                            return Tuple.Create(acc.Item2, next);
                        })
                    .Delay(x => canCreateActivity.Where(b => b))
                    .Subscribe(x =>
                        {
                            var newHead = x.Item2.FirstOrDefault();
                            var oldHead = x.Item1.FirstOrDefault();

                            var newHeadSet = new HashSet<INavigationModel>(x.Item2);
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
                        }),

                this.activityCreated
                    .Subscribe(activity => 
                        {
                            Log.Debug("RxApp", "Activity created of type: " + activity.GetType() + ", with model of type: " + currentModel.GetType());

                            activity.ViewModel = currentModel;
                            activities[ currentModel] = activity;
                            canCreateActivity.Value = true;
                        })
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
        private readonly RxApplicationHelper helper;

        public RxApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            helper = RxApplicationHelper.Create(this.ApplicationContext, this.GetApplication, this.GetActivityType);
        }

        protected abstract IObservable<IEnumerable<INavigationModel>> GetApplication();

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