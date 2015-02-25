using RxApp;
using System;
using System.Reactive.Linq;

namespace RxApp.Example
{      
    public static class MainControllerService
    {
        public static IDisposable Create(IMainControllerModel model)
        {
            return model.OpenPage.Select(_ => new MainModel()).InvokeCommand(model.Open);
        }
    }
}

