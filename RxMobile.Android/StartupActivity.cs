using Android.App;
using Android.OS;
using System;

namespace RxMobile
{
    public sealed class StartupActivity : Activity
    {
        public StartupActivity()
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var app = (ReactiveApplication) this.Application;
            app.OnResume();
            this.Finish();
        }
    }
}

