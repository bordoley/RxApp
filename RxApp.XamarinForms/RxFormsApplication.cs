using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using Xamarin.Forms;

using RxObservable = System.Reactive.Linq.Observable;
using System.Reflection;

namespace RxApp.XamarinForms
{
    public class RxFormsApplication : Application
    {
        public RxFormsApplication()
        {
            base.MainPage = new NavigationPage();
        }

        public new NavigationPage MainPage { get { return (NavigationPage)base.MainPage; } }

        public event EventHandler Done = (o,e) => {};

        public event EventHandler Started = (o,e) => {};

        public event EventHandler Resumed = (o,e) => {};

        public event EventHandler Sleeping = (o,e) => {};

        protected override sealed void OnResume()
        {
            Resumed(this, null);
        }

        protected override sealed void OnSleep()
        {
            Sleeping(this, null);
        }

        protected override sealed void OnStart()
        {
            Started(this, null);
        }

        internal void SendDone()
        {
            Done(this, null);
        }
    }
}

