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
        private IDisposable subscription;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Forms.Init(this, bundle);

            var application = new RxFormsApplication();
            subscription = ExampleApplication.Create(application);

            LoadApplication(application);
        }

        protected override void OnDestroy()
        {
            subscription.Dispose();
            base.OnDestroy();
        }
    }
}


