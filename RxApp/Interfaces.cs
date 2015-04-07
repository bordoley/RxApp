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

    public interface INavigationModel : INavigationViewModel, INavigationControllerModel
    {
        new IObservable<Unit> Back { get; }
        new IObservable<Unit> Up { get; }
        new IObservable<INavigationModel> Open { get; }
    }

    public interface IActivationViewModel 
    {
        IRxCommand Activate { get; }
        IRxCommand Deactivate { get; }
    }

    public interface IActivationControllerModel
    {
        IObservable<Unit> Activate { get; }
        IObservable<Unit> Deactivate { get; }
    }

    public interface IReadOnlyViewFor
    {
        object ViewModel { get; }
    }

    public interface IReadOnlyViewFor<TViewModel> : IReadOnlyViewFor
    {
        new TViewModel ViewModel { get; }
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