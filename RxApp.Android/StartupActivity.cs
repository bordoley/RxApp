using Android.App;
using Android.OS;
using System;
using System.Threading.Tasks;

using Android.Util;
using Android.Views;

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

        public override void OnBackPressed()
        {
            // Swallow back button clicks.

            // Prevent corner cases such as the user slamming
            // the back button on start which could lead to this activity being finished prior
            // to the application state being stabilized.
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            // Swallow up button clicks
            return true;
        }
    }
}

