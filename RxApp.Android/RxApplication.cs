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
            Func<IObservable<INavigationModel>> rootState,
            Func<INavigationControllerModel,IDisposable> bindController,
            Func<INavigationViewModel,Type> getActivityType) 
        {
            Contract.Requires(context != null);
            Contract.Requires(rootState != null);
            Contract.Requires(bindController != null);
            Contract.Requires(getActivityType != null);

            return new RxApplicationHelper(context, rootState, bindController, getActivityType);
        }

        private readonly Context context;

        private readonly Func<IObservable<INavigationModel>> rootState;

        private readonly Func<INavigationControllerModel, IDisposable> bindController;

        private readonly Func<INavigationViewModel,Type> getActivityType;

        private readonly Subject<IRxActivity> activityCreated = new Subject<IRxActivity>();

        private IDisposable subscription;

        private RxApplicationHelper(
            Context context,
            Func<IObservable<INavigationModel>> rootState,
            Func<INavigationControllerModel, IDisposable> bindController,
            Func<INavigationViewModel,Type> getActivityType)
        {
            this.context = context;
            this.rootState = rootState;
            this.bindController = bindController;
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

            var navStack = NavigationStack<INavigationModel>.Create(Observable.MainThreadScheduler);

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
                RxObservable
                    .FromEventPattern<NotifyNavigationStackChangedEventArgs<INavigationModel>>(navStack, "NavigationStackChanged")
                    .Delay(e => canCreateActivity.Where(x => x))
                    .Subscribe(e =>
                        {
                            var newHead = e.EventArgs.NewHead;
                            var oldHead = e.EventArgs.OldHead;
                            var removed = e.EventArgs.Removed;

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
                        }),

                navStack.BindTo(x => bindController(x)),
                    
                rootState().BindTo(navStack.SetRoot)
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
            helper = RxApplicationHelper.Create(this.ApplicationContext, this.RootState, this.BindController, this.GetActivityType);
        }

        public abstract IObservable<INavigationModel> RootState();

        public abstract Type GetActivityType(INavigationViewModel model);

        public abstract IDisposable BindController(INavigationControllerModel model);

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