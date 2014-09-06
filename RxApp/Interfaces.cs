﻿using System;
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

    public interface IInitializableService : IInitializable, IService
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

    public interface IModelStack<TModel> : INotifyPropertyChanged
        where TModel: INavigableModel
    {
        TModel Current { get; }

        void Push(TModel model);
        void SetRoot(TModel model);
    }

    public interface IModelBinder<TModel> : IInitializable
        where TModel : INavigableModel
    {
        IDisposable Bind(IModelStack<TModel> model);
    }
        
    public interface IViewPresenter : IInitializable
    {
        void PresentView(object model);
    }
        
    public interface IControllerProvider : IInitializable
    {
        IInitializable ProvideController(object model);
    }

    public interface ILifecycleViewModel 
    {
        IReactiveCommand<object> Pausing { get; }
        IReactiveCommand<object> Resuming { get; }
    }

    public interface ILifecycleControllerModel
    {
        IObservable<object> Pausing { get; }
        IObservable<object> Resuming { get; }
    }

    public interface ILifecycleModel : ILifecycleViewModel, ILifecycleControllerModel
    {
    }
}

