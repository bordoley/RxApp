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

    public interface INavigableViewModel
    {
        IRxCommand Back { get; }
        IRxCommand Up { get; }
    }

    public interface INavigableControllerModel
    {
        IObservable<Unit> Back { get; }
        IObservable<Unit> Up { get; }
    }

    public interface IServiceViewModel 
    {
        IRxCommand Start { get; }
        IRxCommand Stop { get; }
    }

    public interface IServiceControllerModel
    {
        bool CanStart { set; }

        IObservable<Unit> Start { get; }
        IObservable<Unit> Stop { get; }
    }

    // FIXME: Rework this interface/add extension methods to make it easier to bind it to/from Observables
    public interface INavigationStack : IEnumerable<INavigableControllerModel>
    {
        event EventHandler<NotifyNavigationStackChangedEventArgs> NavigationStackChanged;
       
        void GotoRoot();
        void Pop();
        void Push(INavigableControllerModel model);
        void SetRoot(INavigableControllerModel model);
    }

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