using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using RxApp.Android;
using Android.App;
using Android.Runtime;
using Xamarin;
using System.Reactive.Subjects;

namespace RxApp.Example
{
    [Application]
    public sealed class RxAppExampleApplication : RxApplication
    {
        private IDisposable subscription;

        public RxAppExampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Insights.Initialize("2435d94d2dae17b47f305d2cf5f3413fb1d3aa8d", this.ApplicationContext);

            var builder = new RxAndroidApplicationBuilder();
            builder.NavigationApplicaction = RxAppExampleApplicationController.Create();
            builder.RegisterActivityMapping<IMainViewModel, MainActivity>();
            builder.CreatedActivities = this.CreatedActivities;

            this.subscription = builder.Build().Subscribe();
        }

        public override void OnTerminate()
        {
            this.subscription.Dispose();
            base.OnTerminate();
        }
    }
}