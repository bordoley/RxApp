using System;
using System.Reactive;
using System.Reactive.Subjects;

using Android.App;
using Android.OS;

namespace RxMobile
{        
    public interface IActivityLifecycleEvents
    {
        IObservable<Tuple<Activity,Bundle>> Created { get; }
        IObservable<Activity> Destroyed { get; }
        IObservable<Activity> Paused { get; }
        IObservable<Activity> Resumed { get; }
        IObservable<Tuple<Activity,Bundle>> SaveInstanceState { get; }
        IObservable<Activity> Started { get; }
        IObservable<Activity> Stopped { get; }
    }

    public static class ActivityLifecycleEvents
    {
        public static IActivityLifecycleEvents Register(Application app)
        {
            var retval = new ActivityLifecycleEventsImpl();
            app.RegisterActivityLifecycleCallbacks(retval);
            return retval;
        }
    }

    internal sealed class ActivityLifecycleEventsImpl : Java.Lang.Object, Application.IActivityLifecycleCallbacks, IActivityLifecycleEvents 
    {
        private readonly Subject<Tuple<Activity,Bundle>> created = new Subject<Tuple<Activity,Bundle>>();
        private readonly Subject<Activity> destroyed = new Subject<Activity>();
        private readonly Subject<Activity> paused = new Subject<Activity>();
        private readonly Subject<Activity> resumed = new Subject<Activity>();
        private readonly Subject<Tuple<Activity,Bundle>> saveInstanceState = new Subject<Tuple<Activity,Bundle>>();
        private readonly Subject<Activity> started = new Subject<Activity>();
        private readonly Subject<Activity> stopped = new Subject<Activity>();


        public IObservable<Tuple<Activity, Bundle>> Created
        {
            get
            {
                return created;
            }
        }

        public IObservable<Activity> Destroyed
        {
            get
            {
                return destroyed;
            }
        }

        public IObservable<Activity> Paused
        {
            get
            {
                return paused;
            }
        }

        public IObservable<Activity> Resumed
        {
            get
            {
                return resumed;
            }
        }

        public IObservable<Tuple<Activity, Bundle>> SaveInstanceState
        {
            get
            {
                return saveInstanceState;
            }
        }

        public IObservable<Activity> Started
        {
            get
            {
                return started;
            }
        }

        public IObservable<Activity> Stopped
        {
            get
            {
                return stopped;
            }
        }

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
            created.OnNext(Tuple.Create(activity, savedInstanceState));
        }

        public void OnActivityResumed(Activity activity)
        {
            resumed.OnNext(activity);
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
            saveInstanceState.OnNext(Tuple.Create(activity, outState));
        }

        public void OnActivityPaused(Activity activity) 
        { 
            paused.OnNext(activity);
        }

        public void OnActivityDestroyed(Activity activity) 
        { 
            destroyed.OnNext(activity);
        }

        public void OnActivityStarted(Activity activity) 
        { 
            started.OnNext(activity);
        }

        public void OnActivityStopped(Activity activity) 
        { 
            stopped.OnNext(activity);
        }
    }
}