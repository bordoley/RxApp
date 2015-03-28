using System;
using System.Collections.Generic;
using System.Reactive;

namespace RxApp
{    
    public interface INavigationViewModel : IActivationViewModel 
    {
        IRxCommand Back { get; }
        IRxCommand Up { get; }
    }

    public interface INavigationControllerModel : IActivationControllerModel
    {
        IRxCommand Back { get; }
        IRxCommand Up { get; }
        IRxCommand<INavigationModel> Open { get; }
    }

    public interface INavigationStackModel<T>
        where T : INavigationStackModel<T>
    {
        IObservable<Unit> Back { get; }
        IObservable<Unit> Up { get; }
        IObservable<T> Open { get; }
    }

    public interface INavigationModel : INavigationViewModel, INavigationControllerModel, INavigationStackModel<INavigationModel>
    {
    }

    public interface INavigationController
    {
        IObservable<INavigationModel> RootState { get; }

        IDisposable Bind(INavigationControllerModel model);
    }

    public interface IActivationViewModel 
    {
        IRxCommand Activate { get; }
        IRxCommand Deactivate { get; }
    }

    public interface IActivationControllerModel
    {
        IRxProperty<bool> CanActivate { get; }

        IObservable<Unit> Activate { get; }
        IObservable<Unit> Deactivate { get; }
    }

    public interface IViewFor
    {
        object ViewModel { get; set; }
    }

    public interface IViewFor<TViewModel> : IViewFor
    {
        new TViewModel ViewModel { get; set; }
    }
}