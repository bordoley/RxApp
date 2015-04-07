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
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Forms.Init();

            var application = new RxFormsApplication();
            var exampleApp = ExampleApplication.Create(application);
            exampleApp.Subscribe();

            LoadApplication(application);

            return base.FinishedLaunching (app, options);
        }
    }
}

