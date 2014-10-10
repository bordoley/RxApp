using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using Android.App;
using Android.Runtime;

namespace RxApp.Example.Android
{
    [Application]
    public sealed class RxAppExampleApplication : RxApplication
    {
        private readonly RxAppExampleApplicationController applicationController;

        public RxAppExampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            applicationController = new RxAppExampleApplicationController(this.NavigationStack);
        }

        public override Type GetActivityType(object model)
        {
            // This is a lot prettier in F# using pattern matching
            if (model is IMainViewModel)
            {
                return typeof(MainActivity);
            } 

            throw new Exception("No view for view model");
        }

        public override IDisposable ProvideController(object model)
        {
            return applicationController.Bind(model);
        }

        public override void Start()
        {
            applicationController.Start();
        }

        public override void Stop()
        {
            applicationController.Stop();
        }
    }
}