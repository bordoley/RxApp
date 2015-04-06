using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Foundation;
using UIKit;

using RxApp;
using RxApp.iOS;
using System.Reactive.Subjects;

namespace RxApp.Example
{
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        private IDisposable appSubscription;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var storyBoard = UIStoryboard.FromName("Views", null);
            var builder = new RxiOSApplicationBuilder();
            builder.NavigationApplicaction = RxAppExampleApplicationController.Create();
            builder.RegisterViewCreator<IMainViewModel,ExampleViewController>(model =>
                {
                    var view = (ExampleViewController) storyBoard.InstantiateViewController("ExampleViewController");
                    view.ViewModel = model;
                    return view;
                });
            appSubscription = builder.Build().Subscribe();
            return true;
        }

        public override void WillTerminate(UIApplication app)
        {
            appSubscription.Dispose();
        }
    }
}
