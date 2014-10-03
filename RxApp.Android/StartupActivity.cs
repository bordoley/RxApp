using Android.App;
using Android.OS;
using System;
using System.Threading.Tasks;

namespace RxApp
{
    public sealed class StartupActivity : Activity
    {
        private static bool isColdBoot = true;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (isColdBoot)
            {
                isColdBoot = false;

            }
            else
            {
                await Task.Delay(new TimeSpan(10000000));
            }

            StartApp();
        }

        private void StartApp()
        { 
            var app = (IService) this.Application;
            app.Start();
            this.Finish();
        }
    }
}

