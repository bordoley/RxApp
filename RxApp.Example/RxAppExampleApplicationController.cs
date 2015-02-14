﻿using RxApp;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxApp.Example
{
    public class RxAppExampleApplicationController : IApplication
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

                Func<IDisposable> service = () =>
                {
                    var ret = new MainControllerService((IMainControllerModel)model, navStack);
                    ret.Init();
                    return ret;
                };

                return (model as IServiceControllerModel).Bind(service);
            }
            else
            {
                return Disposable.Empty;
            }
        }

        public void Init()
        {
            navStack.Push(new MainModel());
        }

        public void Dispose()
        {
        }
    }
}

