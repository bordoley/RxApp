using RxApp;
using System;
using System.Reactive.Linq;

namespace RxApp.Example
{      
    public class MainControllerService : IDisposable
    {
        private readonly IMainControllerModel model;

        private IDisposable subscription = null;

        public MainControllerService(IMainControllerModel model)
        {
            this.model = model;
        }

        public void Init()
        {
            subscription = 
                model.OpenPage.Select(_ => new MainModel()).InvokeCommand(model.Open);
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

