using System;
using System.Collections.Generic;
using System.Reactive;
using System.Windows.Input;

namespace RxApp
{
    public interface IApplication : IDisposable
    {
        void Init();

        IDisposable Bind(object controllerModel);
    }

    public interface INavigationModel
    {
        IRxCommand Back { get; }
        IRxCommand Up { get; }
        IRxCommand<INavigationModel> Open { get; }
    }
      
    public interface IServiceViewModel 
    {
        IRxCommand Start { get; }
        IRxCommand Stop { get; }
    }

    public interface IServiceControllerModel
    {
        IRxProperty<bool> CanStart { get; }

        IObservable<Unit> Start { get; }
        IObservable<Unit> Stop { get; }
    }

    // FIXME: Rework this interface/add extension methods to make it easier to bind it to/from Observables
    public interface INavigationStack : IEnumerable<INavigationModel>
    {
        event EventHandler<NotifyNavigationStackChangedEventArgs> NavigationStackChanged;
        void SetRoot(INavigationModel model);
    }

    // Fixme: Not sure these should really be in core. They're sort of convenience
    // since on android and ios you can't do constructor injection sanely.
    public interface IViewFor
    {
        object ViewModel { get; set; }
    }

    public interface IViewFor<TViewModel> : IViewFor
        where TViewModel: class
    {
        new TViewModel ViewModel { get; set; }
    }
}