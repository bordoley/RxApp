using ReactiveUI;
using RxApp;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxApp.Example
{
    public class RxAppExampleApplicationController : ReactiveObject, IService
    {
        private readonly INavigationStack navStack;

        private bool isStarted = false;

        public RxAppExampleApplicationController(INavigationStack navStack)
        {
            this.navStack = navStack;
        }
            
        public Boolean IsStarted
        {
            get { return isStarted; }
            set { this.RaiseAndSetIfChanged(ref isStarted, value); }
        }

        public IDisposable Bind(object model)
        {
            // This is a lot prettier if you use F# pattern matching
            if (model is IMainControllerModel)
            {

                var service = new MainControllerService((IMainControllerModel)model, navStack);
                return (model as IServiceControllerModel).BindAndDispose(service);
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

