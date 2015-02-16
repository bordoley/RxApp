using System;

using Android.App;
using Android.OS;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

using ReactiveUI;
using RxApp;

namespace RxApp.Example
{
    [Activity(Label = "MainActivity")]            
    public sealed class MainActivity : RxActionBarActivity<IMainViewModel>
    {
        private IDisposable subscription = null;
        private Button button;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Main);

            button = this.FindViewById<Button>(Resource.Id.myButton);

            var toolbar = FindViewById<Toolbar> (Resource.Id.toolbar);

            //Toolbar will now take on default actionbar characteristics
            SetSupportActionBar (toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled (true);
            SupportActionBar.SetHomeButtonEnabled (true);
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

