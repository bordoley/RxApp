using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Android.App;
using Android.OS;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

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
            // Update the activity theme. Must be the first thing done in OnCreate();
            this.SetTheme(Resource.Style.RxAppTheme);

            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Main);

            button = this.FindViewById<Button>(Resource.Id.myButton);

            var toolbar = FindViewById<Toolbar> (Resource.Id.toolbar);
            SetSupportActionBar (toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled (true);
            SupportActionBar.SetHomeButtonEnabled (true);
        }

        protected override void OnResume()
        {
            base.OnResume();

            var subscription = new CompositeDisposable();

            // FIXME: Need to add some sort of simple binding layer.
            subscription.Add(
                this.ViewModel.OpenPage.CanExecuteObservable.Subscribe(x => this.button.Enabled = x));
            subscription.Add(
                Observable.FromEventPattern(this.button, "Click").InvokeCommand(this.ViewModel.OpenPage));

            this.subscription = subscription;
        }

        protected override void OnPause()
        {
            subscription.Dispose();
            base.OnPause();
        }
    }
}

