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
        private IDisposable subscription = null;

		public ExampleViewController (IntPtr handle) : base (handle)
		{
		}

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            subscription = Disposable.Combine(
                this.ViewModel.OpenPage.Bind(this.OpenButton),
                this.ViewModel.Up.Bind(this.UpButton)
            );
        }

        public override void ViewDidDisappear(bool animated)
        {
            subscription.Dispose();
            base.ViewDidDisappear(animated);
        }
	}
}
