using ReactiveUI;
using RxApp;
using System;

namespace RxApp.Example.Android
{      
    public class MainController : IInitializable
    {
        private readonly IMainControllerModel model;
        private readonly INavigationStackControllerModel<IMobileModel> navStack;

        private IDisposable subscription = null;

        public MainController(IMainControllerModel model, INavigationStackControllerModel<IMobileModel> navStack)
        {
            this.model = model;
            this.navStack = navStack;
        }

        public void Initialize()
        {
            subscription = 
                model.OpenPage.Subscribe(_ => 
                    navStack.Push(new MainModel()));
        }

        public void Dispose()
        {
            subscription.Dispose();
        }
    }
}

