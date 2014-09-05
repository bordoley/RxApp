using Android.App;
using Android.OS;
using System;

namespace RxMobile
{
    public sealed class StartupActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var app = (RxMobileApplication) this.Application;
            app.Run();
            this.Finish();
        }
    }
}

