using System;
using System.Reactive;
using System.Windows.Input;

namespace RxApp
{
    public interface INavigableViewModel
    {
        ICommand Back { get; }
        ICommand Up { get; }
    }

    public interface INavigableControllerModel
    {
        IObservable<Unit> Back { get; }
        IObservable<Unit> Up { get; }
    }
}

