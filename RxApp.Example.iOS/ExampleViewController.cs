using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;
using System.Reactive.Disposables;

using ReactiveUI;
using RxApp;

namespace RxApp.Example
{
    partial class ExampleViewController : RxUIViewController<IMainViewModel>
	{
        private CompositeDisposable subscription = null;

		public ExampleViewController (IntPtr handle) : base (handle)
		{
		}

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            subscription = new CompositeDisposable();

            subscription.Add( 
                this.BindCommand(
                    this.ViewModel, 
                    vm => vm.OpenPage,
                    view => view.OpenButton));

            subscription.Add( 
                this.BindCommand(
                    this.ViewModel, 
                    vm => vm.Up,
                    view => view.UpButton));
        }

        public override void ViewDidDisappear(bool animated)
        {
            subscription.Dispose();
            base.ViewDidDisappear(animated);
        }
	}
}
