using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

using RxApp;
using RxApp.iOS;

namespace RxApp.Example
{
    [Register("AppDelegate")]
    public partial class AppDelegate : RxUIApplicationDelegate
    {
        public AppDelegate()
        {
            var storyBoard = UIStoryboard.FromName("Views", null);
            this.RegisterViewCreator<IMainViewModel,ExampleViewController>(model =>
                {
                    var view = (ExampleViewController) storyBoard.InstantiateViewController("ExampleViewController");
                    (view as IViewFor).ViewModel = model;
                    return view;
                });
        }

        protected override IObservable<IEnumerable<INavigationModel>> GetApplication()
        { 
            return RxAppExampleApplicationController.Create();
        }
    }
}
