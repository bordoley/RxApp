using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using RxApp.XamarinForms;

namespace RxApp.Example.XamarinForms
{
    [Activity(Label = "RxApp.Example.XamarinForms.Android", MainLauncher = true)]
    public class MainActivity : RxFormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Forms.Init(this, bundle);

            var application = new RxFormsApplication();
            var exampleApp = ExampleApplication.Create(application);
            exampleApp.Subscribe();

            LoadApplication(application);
        }
    }
}


