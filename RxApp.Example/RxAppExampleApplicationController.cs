using RxApp;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxApp.Example
{
    public class RxAppExampleApplicationController
    {
        private readonly INavigationStack<IMobileModel> navStack;

        public RxAppExampleApplicationController(INavigationStack<IMobileModel> navStack)
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

