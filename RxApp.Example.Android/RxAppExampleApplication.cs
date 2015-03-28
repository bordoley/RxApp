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

namespace RxApp.Example
{
    [Application]
    public sealed class RxAppExampleApplication : RxApplication
    {
        public RxAppExampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            this.RegisterActivity<IMainViewModel, MainActivity>();
        }

        protected override IObservable<INavigationModel> RootState()
        { 
            return RxAppExampleApplicationController.RootState;
        }

        protected override IDisposable BindController(INavigationControllerModel model)
        {
            return RxAppExampleApplicationController.Bind(model);
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Insights.Initialize("2435d94d2dae17b47f305d2cf5f3413fb1d3aa8d", this.ApplicationContext);
        }
    }
}