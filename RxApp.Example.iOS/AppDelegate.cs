using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using ReactiveUI;

namespace RxApp.Example.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        private readonly INavigationStack navStack;
        private readonly RxAppExampleApplicationController applicationController;
        private readonly RxUIApplicationDelegateHelper helper;

        public AppDelegate()
        {
            navStack = NavigationStack.Create();
            applicationController = new RxAppExampleApplicationController(navStack);
            helper = RxUIApplicationDelegateHelper.Create(navStack, applicationController, applicationController.Bind, model =>
                {
                    // This is a lot prettier in F# using pattern matching
                    if (model is IMainViewModel)
                    {
                        var view = UIStoryboard.FromName("Views", null).InstantiateViewController("ExampleViewController");
                        (view as IViewFor).ViewModel = model;
                        return view as UIViewController;
                    } 

                    throw new Exception("No view for view model");
                });
        }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            return helper.FinishedLaunching(app, options);
        }

        public override void WillTerminate(UIApplication app)
        {
            helper.WillTerminate(app);
        }
    }
}

