using System;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using System.CodeDom.Compiler;
using System.Reactive.Disposables;

using RxApp;

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
            var subscription = new CompositeDisposable();

            // FIXME: Need to add some sort of simple binding layer.
            subscription.Add(
                this.ViewModel.OpenPage.CanExecuteObservable.Subscribe(x => this.OpenButton.Enabled = x));
            subscription.Add(
                Observable.FromEventPattern(this.OpenButton, "TouchUpInside").InvokeCommand(this.ViewModel.OpenPage));

            subscription.Add(
                this.ViewModel.Up.CanExecuteObservable.Subscribe(x => this.UpButton.Enabled = x));
            subscription.Add(
                Observable.FromEventPattern(this.UpButton, "TouchUpInside").InvokeCommand(this.ViewModel.Up)); 

            this.subscription = subscription;
        }

        public override void ViewDidDisappear(bool animated)
        {
            subscription.Dispose();
            base.ViewDidDisappear(animated);
        }
	}
}
