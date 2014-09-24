using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using Android.App;
using Android.Runtime;

namespace RxApp.Example.Android
{
    [Application]
    public sealed class RxAppExampleApplication : RxAndroidApplication
    {
        private readonly IMobileApplicationController applicationController;

        public RxAppExampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            applicationController = new RxAppExampleApplicationController(this.NavigationStack);
        }

        public override IMobileApplicationController ApplicationController
        {
            get
            {
                return applicationController;
            }
        }

        public override Type GetViewType(object model)
        {
            // This is a lot prettier in F# using pattern matching
            if (model is IMainViewModel)
            {
                return typeof(MainActivity);
            } 

            throw new Exception("No view for view model");
        }
    }

    internal sealed class RxAppExampleApplicationController : IMobileApplicationController
    {
        private readonly INavigationStackControllerModel<IMobileModel> navStack;

        internal RxAppExampleApplicationController(INavigationStackControllerModel<IMobileModel> navStack)
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

        public void Start()
        {
            navStack.Push(new MainModel());
        }

        public void Stop()
        {
        }
    }
}

