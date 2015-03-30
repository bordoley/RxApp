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
    public partial class AppDelegate : RxUIApplicationDelegate
    {
        public AppDelegate()
        {
            var storyBoard = UIStoryboard.FromName("Views", null);
            this.RegisterViewCreator<IMainViewModel,ExampleViewController>(model =>
                {
                    var view = (ExampleViewController) storyBoard.InstantiateViewController("ExampleViewController");
                    view.ViewModel = model;
                    return view;
                });
        }

        protected override IConnectableObservable<ImmutableStack<INavigationModel>> BuildNavigationApplication()
        { 
            return RxAppExampleApplicationController.Create();
        }
    }
}
