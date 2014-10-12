using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;

using ReactiveUI;

namespace RxApp.Example.iOS
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

            subscription = 
                this.BindCommand(
                    this.ViewModel, 
                    vm => vm.OpenPage,
                    view => view.OpenButton);
        }

        public override void ViewDidDisappear(bool animated)
        {
            subscription.Dispose();
            base.ViewDidDisappear(animated);
        }
	}
}
