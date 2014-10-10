using System;
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
        IObservable<object> Back { get; }
        IObservable<object> Up { get; }
    }
}

