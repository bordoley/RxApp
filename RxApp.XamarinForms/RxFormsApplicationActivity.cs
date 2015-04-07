using System;
using Xamarin.Forms.Platform.Android;

namespace RxApp.XamarinForms
{
    public abstract class RxFormsApplicationActivity : FormsApplicationActivity
    {
        public RxFormsApplicationActivity()
        {
        }

        public void LoadApplication(RxFormsApplication application)
        {
            base.LoadApplication(application);
            application.Done += (o,e) => this.Finish();
        }
    }
}

