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
        private IDisposable subscription;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var storyBoard = UIStoryboard.FromName("Views", null);

            var viewCreatorBuilder = new ViewCreatorBuilder();
            viewCreatorBuilder.RegisterViewCreator<IMainViewModel,ExampleViewController>(model =>
                {
                    var view = (ExampleViewController) storyBoard.InstantiateViewController("ExampleViewController");
                    view.ViewModel = model;
                    return view;
                });

            var navigationController = new RxUINavigationController();
            subscription = RxAppExampleApplicationController.Create().BindTo(navigationController, viewCreatorBuilder.Build());

            var window = new UIWindow(UIScreen.MainScreen.Bounds);
            window.RootViewController = navigationController;
            window.MakeKeyAndVisible();

            return true;
        }

        public override void WillTerminate(UIApplication app)
        {
            subscription.Dispose();
        }
    }
}
