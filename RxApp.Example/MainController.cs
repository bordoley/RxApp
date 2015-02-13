using RxApp;
using System;

namespace RxApp.Example
{      
    public class MainControllerService : IDisposable
    {
        private readonly IMainControllerModel model;
        private readonly INavigationStack navStack;

        private IDisposable subscription = null;

        public MainControllerService(IMainControllerModel model, INavigationStack navStack)
        {
            this.model = model;
            this.navStack = navStack;
        }

        public void Init()
        {
            subscription = 
                model.OpenPage.Subscribe(_ => 
                    navStack.Push(new MainModel()));
        }

        public void Dispose()
        {
            if (subscription != null)
            {
                subscription.Dispose();
                subscription = null;
            }
        }
    }
}

