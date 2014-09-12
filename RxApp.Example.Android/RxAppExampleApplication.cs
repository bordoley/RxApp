using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using RxApp;
using Android.App;
using Android.Runtime;

namespace RxApp.Example.Android
{
    [Application]
    public sealed class RxAppExampleApplication : RxAndroidApplicationBase
    {
        public RxAppExampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override Type GetViewType(IMobileViewModel model)
        {
            // This is a lot prettier in F# using pattern matching
            if (model is IMainViewModel)
            {
                return typeof(MainActivity);
            } 

            throw new Exception("No view for view model");
        }

        protected override IModelBinder<IMobileControllerModel> ProvideControllerBinder()
        {
            return new ExampleModelBinder(this.NavigationStack);
        }
    }

    internal class ExampleModelBinder : IModelBinder<IMobileControllerModel>
    {
        private readonly INavigationStackControllerModel<IMobileModel> navStack;

        internal ExampleModelBinder(INavigationStackControllerModel<IMobileModel> navStack)
        {
            this.navStack = navStack;
        }

        public IDisposable Bind(IMobileControllerModel model)
        {

            // This is a lot prettier if you use F# pattern matching
            if (model is IMainControllerModel)
            {
                return model.Bind(new MainControllerService((IMainControllerModel) model, navStack));
            }
            else
            {
                return Disposable.Empty;
            }
        }

        public void Initialize()
        {
            // A good place to start dependendent services like network status monitoring
            // that is shared by all controllers this binder binds models too.

            // Push the initial state of the app onto the nav stack
            navStack.Push(new MainModel());
        }

        public void Dispose()
        {
            // Dispose of any services that have been initialized here.
        }
    }
}

