using RxApp;
using System;
using System.Reactive;
using System.Windows.Input;

namespace RxApp.Example
{   
    public interface IMainViewModel : INavigationViewModel
    {
        IRxCommand OpenPage { get; }
    }

    public interface IMainControllerModel : INavigationControllerModel
    {
        IObservable<Unit> OpenPage { get; }
    }

    public class MainModel : NavigationModel, IMainViewModel, IMainControllerModel
    {
        private readonly IRxCommand openPage = RxCommand.Create();

        public IRxCommand OpenPage { get { return openPage; } }

        IObservable<Unit> IMainControllerModel.OpenPage { get { return openPage; } }
    }
}

