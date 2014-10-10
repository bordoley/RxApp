using ReactiveUI;
using RxApp;
using System;

namespace RxApp.Example
{      
    public class MainControllerService : ReactiveObject, IService
    {
        private readonly IMainControllerModel model;
        private readonly INavigationStack navStack;

        private bool isStarted = false;
        private IDisposable subscription = null;

        public MainControllerService(IMainControllerModel model, INavigationStack navStack)
        {
            this.model = model;
            this.navStack = navStack;
        }

        public bool IsStarted 
        { 
            get { return isStarted; }
            private set { this.RaiseAndSetIfChanged(ref isStarted, true); }  
        }

        public void Start()
        {
            subscription = 
                model.OpenPage.Subscribe(_ => 
                    navStack.Push(new MainModel()));

            this.IsStarted = true;
        }

        public void Stop()
        {
            subscription.Dispose();
            subscription = null;

            this.IsStarted = false;
        }
    }
}

