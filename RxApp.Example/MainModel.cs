using ReactiveUI;
using RxApp;
using System;

namespace RxApp.Example
{   
    public interface IMainViewModel : INavigableViewModel, IServiceViewModel
    {
        IReactiveCommand<object> OpenPage { get; }
    }

    public interface IMainControllerModel
    {
        IObservable<object> OpenPage { get; }
    }

    public class MainModel : MobileModel, IMainViewModel, IMainControllerModel
    {
        private readonly IReactiveCommand<object> openPage = ReactiveCommand.Create();

        public IReactiveCommand<object> OpenPage { get { return openPage; } }

        IObservable<object> IMainControllerModel.OpenPage { get { return (IObservable<object>) openPage; } }
    }
}

