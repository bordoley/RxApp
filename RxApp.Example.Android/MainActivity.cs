using Android.App;
using Android.OS;
using Android.Widget;

using System;
using ReactiveUI;
using RxApp;

namespace RxApp.Example.Android
{
    [Activity(Label = "MainActivity")]            
    public sealed class MainActivity : RxActivity<IMainViewModel>
    {
        private IDisposable subscription = null;
        private Button button;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Main);
            this.ActionBar.SetDisplayHomeAsUpEnabled(true);

            button = this.FindViewById<Button>(Resource.Id.myButton);
        }

        protected override void OnResume()
        {
            base.OnResume();

            subscription = 
                this.BindCommand(
                    this.ViewModel, 
                    vm => vm.OpenPage,
                    view => view.button);
        }

        protected override void OnPause()
        {
            subscription.Dispose();
            base.OnPause();
        }
    }
}

