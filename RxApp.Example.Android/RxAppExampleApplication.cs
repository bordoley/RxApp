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
    public sealed class RxAppExampleApplication : ActivityViewApplication
    {
        private readonly RxAppExampleApplicationController applicationController;

        public RxAppExampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            applicationController = new RxAppExampleApplicationController(this.NavigationStack);
        }

        public override Type GetActivityType(IMobileViewModel model)
        {
            // This is a lot prettier in F# using pattern matching
            if (model is IMainViewModel)
            {
                return typeof(MainActivity);
            } 

            throw new Exception("No view for view model");
        }

        public override IDisposable ProvideController(IMobileControllerModel model)
        {
            return applicationController.Bind(model);
        }

        public override void Start()
        {
            applicationController.Start();
        }

        public override void Stop()
        {
            applicationController.Stop();
        }
    }

    internal sealed class RxAppExampleApplicationController
    {
        private readonly INavigationStack<IMobileModel> navStack;

        internal RxAppExampleApplicationController(INavigationStack<IMobileModel> navStack)
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