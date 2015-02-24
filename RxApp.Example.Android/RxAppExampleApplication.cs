﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using RxApp.Android;
using Android.App;
using Android.Runtime;
using Xamarin;

namespace RxApp.Example
{
    [Application]
    public sealed class RxAppExampleApplication : RxApplication
    {
        public RxAppExampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {

        }

        public override IObservable<IMobileModel> RootState()
        { 
            return RxAppExampleApplicationController.RootState;
        }

        public override IDisposable BindController(object model)
        {
            return RxAppExampleApplicationController.Bind(model);
        }

        public override Type GetActivityType(IMobileViewModel model)
        {
            // This is a lot prettier in F# using pattern matching
            if (model is IMainViewModel)
            {
                return typeof(MainActivity);
            } 

            throw new Exception("No view for view model");
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Insights.Initialize("2435d94d2dae17b47f305d2cf5f3413fb1d3aa8d", this.ApplicationContext);
        }
    }
}