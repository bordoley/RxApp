﻿using RxApp;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxApp.Example
{
    public class RxAppExampleApplicationController
    {
        private readonly INavigationStack navStack;

        public RxAppExampleApplicationController(INavigationStack navStack)
        {
            this.navStack = navStack;
        }

        public IDisposable Bind(object model)
        {
            // This is a lot prettier if you use F# pattern matching
            if (model is IMainControllerModel)
            {
                IService service = new MainControllerService((IMainControllerModel)model, navStack);
                return ((IServiceControllerModel) model).Bind(service);
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
