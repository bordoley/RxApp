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
    public interface IRxApplication
    {
        IObservable<IViewFor> WhenActivityCreated { get; }

        void OnActivityCreated<TActivity>(TActivity activity) where TActivity: Activity, IViewFor;
    }

    public abstract class RxApplication : Application, IRxApplication
    {
        private readonly Subject<IViewFor> whenActivityCreated = new Subject<IViewFor>();


        public RxApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public IObservable<IViewFor> WhenActivityCreated { get { return whenActivityCreated; } }

        public void OnActivityCreated<TActivity>(TActivity activity) 
            where TActivity: Activity, IViewFor
        {
            whenActivityCreated.OnNext(activity);
        }
    }
}