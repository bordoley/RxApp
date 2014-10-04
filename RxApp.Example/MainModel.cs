using ReactiveUI;
using RxApp;
using System;

namespace RxApp.Example
{   
    public interface IMainViewModel : IMobileViewModel
    {
        IReactiveCommand<object> OpenPage { get; }
    }

    public interface IMainControllerModel : IMobileControllerModel
    {
        IObservable<object> OpenPage { get; }
    }

    public interface IMainModel : IMainViewModel, IMainControllerModel, IMobileModel
    {
    }

    public class MainModel : MobileModel, IMainModel
    {
        private readonly IReactiveCommand<object> openPage = ReactiveCommand.Create();

        public IReactiveCommand<object> OpenPage { get { return openPage; } }
        IObservable<object> IMainControllerModel.OpenPage { get { return (IObservable<object>) openPage; } }
    }
}

