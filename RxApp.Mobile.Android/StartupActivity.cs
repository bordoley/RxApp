using Android.App;
using Android.OS;
using System;

namespace RxApp
{
    public sealed class StartupActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var app = (IRxAndroidApplication) this.Application;
            app.Start();
            this.Finish();
        }
    }
}

