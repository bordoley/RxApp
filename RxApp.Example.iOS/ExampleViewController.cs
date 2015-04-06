using System;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using System.CodeDom.Compiler;
using System.Reactive.Disposables;

using RxApp;
using RxApp.iOS;

using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp.Example
{
    partial class ExampleViewController : RxUIViewController<IMainViewModel>
    {   
        private readonly UIBarButtonItem navbarUpButton;

        private IDisposable subscription = null;

        public ExampleViewController (IntPtr handle) : base (handle)
        {
            navbarUpButton = new UIBarButtonItem();
            navbarUpButton.Title = "Up";
            this.NavigationItem.RightBarButtonItem = navbarUpButton;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            subscription = Disposable.Compose(
                this.ViewModel.OpenPage.Bind(this.OpenButton),
                this.ViewModel.Up.Bind(this.navbarUpButton)
            );
        }

        public override void ViewDidDisappear(bool animated)
        {
            subscription.Dispose();
            base.ViewDidDisappear(animated);
        }
    }
}
