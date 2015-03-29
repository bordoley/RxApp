using System;
using System.Collections.Generic;
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
        private readonly IConnectableObservable<IEnumerable<INavigationModel>> application;

        public RxAppExampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            this.RegisterActivity<IMainViewModel, MainActivity>();
            this.application = RxAppExampleApplicationController.Create();
        }

        protected override IConnectableObservable<IEnumerable<INavigationModel>> NavigationApplicaction
        { 
            get { return application; }
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Insights.Initialize("2435d94d2dae17b47f305d2cf5f3413fb1d3aa8d", this.ApplicationContext);
        }
    }
}