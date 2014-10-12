using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using Android.App;
using Android.Runtime;

namespace RxApp.Example
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

        public override void OnCreate()
        {
            base.OnCreate();

            AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
                {
                    var path = Context.GetExternalFilesDir("exceptions").Path;
                    StreamWriter file = File.CreateText(path, "exception.txt");
                    file.Write(args.Exception.StackTrace); // save the exception description and clean stack trace
                    file.Close();
                };
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