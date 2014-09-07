using System;
using System.ComponentModel;
using ReactiveUI;

namespace RxApp
{
    public interface IInitializable : IDisposable
    {
        void Initialize();
    }

    public interface IService
    {
        void Start();
        void Stop();
    }

    public interface IDisposableService : IDisposable, IService
    {
    }

    public interface INavigableViewModel
    {
        IReactiveCommand<object> Back { get; }
        IReactiveCommand<object> Up { get; }
        IObservable<object> Close { get; }
    }

    public interface INavigableControllerModel
    {
        IObservable<object> Back { get; }
        IObservable<object> Up { get; }
        IReactiveCommand<object> Close { get; }
    }

    public interface INavigableModel : INavigableViewModel, INavigableControllerModel
    {
    }

    public interface INavigationStackViewModel<TModel> : INotifyPropertyChanged
    {
        TModel Current { get; }
    }

    public interface INavigationStackControllerModel<TModel> 
        where TModel: INavigableModel
    {
        void Push(TModel model);
        void SetRoot(TModel model);
    }

    public interface INavigationStackModel<TModel> : INavigationStackViewModel<TModel>, INavigationStackControllerModel<TModel> 
        where TModel: INavigableModel
    {
    }

    public interface INavigationViewController : IInitializable
    {
        void PresentView(object model);
        IDisposable AttachController(object model);
    }

    public interface IServiceViewModel 
    {
        IReactiveCommand<object> Start { get; }
        IReactiveCommand<object> Stop { get; }
    }

    public interface IServiceControllerModel
    {
        IObservable<object> Start { get; }
        IObservable<object> Stop { get; }
    }

    public interface IServiceModel : IServiceViewModel, IServiceControllerModel
    {
    }
}

