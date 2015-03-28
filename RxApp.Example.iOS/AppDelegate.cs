using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

using RxApp.iOS;

namespace RxApp.Example
{
    [Register("AppDelegate")]
    public partial class AppDelegate : RxUIApplicationDelegate
    {
        public AppDelegate()
        {
            var storyBoard = UIStoryboard.FromName("Views", null);
            this.RegisterViewCreator<IMainViewModel,ExampleViewController>(() =>
                (ExampleViewController) storyBoard.InstantiateViewController("ExampleViewController"));
        }

        protected override IObservable<INavigationModel> RootState()
        { 
            return RxAppExampleApplicationController.RootState;
        }

        protected override IDisposable BindController(INavigationControllerModel model)
        {
            return RxAppExampleApplicationController.Bind(model);
        }
    }
}
