using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using RxApp.XamarinForms;

namespace RxApp.Example.XamarinForms.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        private IDisposable subscription;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Forms.Init();

            var application = new RxFormsApplication();
            subscription = ExampleApplication.Create(application);

            LoadApplication(application);

            return base.FinishedLaunching (app, options);
        }

        public override void WillTerminate(UIApplication app)
        {
            subscription.Dispose();
        }
    }
}

