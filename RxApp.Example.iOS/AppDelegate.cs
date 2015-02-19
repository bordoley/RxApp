using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace RxApp.Example
{
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        private readonly RxUIApplicationDelegateHelper helper;

        public AppDelegate()
        {
            helper = 
                RxUIApplicationDelegateHelper.Create(
                    navStack => new RxAppExampleApplicationController(navStack), 
                    model =>
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
