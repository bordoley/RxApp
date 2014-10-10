using ReactiveUI;
using RxApp;
using System;

namespace RxApp.Example
{      
    public class MainControllerService : IService
    {
        private readonly IMainControllerModel model;
        private readonly INavigationStack navStack;

        private IDisposable subscription = null;

        public MainControllerService(IMainControllerModel model, INavigationStack navStack)
        {
            this.model = model;
            this.navStack = navStack;
        }

        public void Start()
        {
            subscription = 
                model.OpenPage.Subscribe(_ => 
                    navStack.Push(new MainModel()));
        }

        public void Stop()
        {
            subscription.Dispose();
            subscription = null;
        }
    }
}

