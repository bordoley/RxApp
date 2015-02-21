using Android.App;
using Android.OS;
using System;
using System.Threading.Tasks;

using Android.Util;

namespace RxApp.Android
{
    public sealed class StartupActivity : Activity, IRxActivity
    {
        private static bool isColdBoot = true;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (isColdBoot)
            {
                isColdBoot = false;
                StartApp();
            }
            else
            {
                await Task.Delay(new TimeSpan(0, 0, 1));
                StartApp();
            }
        }

        public object ViewModel
        {
            get { return null; }

            set {  }
        }

        private void StartApp()
        { 
            var app = (IRxApplication) this.Application;
            app.OnActivityCreated(this);
        }
    }
}

