// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;

namespace RxApp.Example
{
	[Register ("ExampleViewController")]
	partial class ExampleViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIButton OpenButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIButton UpButton { get; set; }

		void ReleaseDesignerOutlets ()
		{
			if (OpenButton != null) {
				OpenButton.Dispose ();
				OpenButton = null;
			}
			if (UpButton != null) {
				UpButton.Dispose ();
				UpButton = null;
			}
		}
	}
}
