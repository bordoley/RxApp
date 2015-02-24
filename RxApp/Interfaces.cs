using System;
using System.Collections.Generic;
using System.Reactive;

namespace RxApp
{
    public interface INavigationModel
    {
        IRxCommand Back { get; }
        IRxCommand Up { get; }
        IRxCommand<INavigationModel> Open { get; }
    }
      
    public interface IActivationViewModel 
    {
        IRxCommand Start { get; }
        IRxCommand Stop { get; }
    }

    public interface IActivationControllerModel
    {
        IRxProperty<bool> CanStart { get; }

        IObservable<Unit> Start { get; }
        IObservable<Unit> Stop { get; }
    }

    // Fixme: Not sure these should really be in core. They're sort of convenience
    // since on android and ios you can't do constructor injection sanely.
    public interface IViewFor
    {
        object ViewModel { get; set; }
    }

    public interface IViewFor<TViewModel> : IViewFor
    {
        new TViewModel ViewModel { get; set; }
    }
}