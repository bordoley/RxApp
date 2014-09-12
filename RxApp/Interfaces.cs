using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
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
    }

    public interface INavigableControllerModel
    {
        IObservable<object> Back { get; }
        IObservable<object> Up { get; }
    }

    public interface INavigableModel : INavigableViewModel, INavigableControllerModel
    {

    }

    public sealed class NotifyNavigationStackChangedEventArgs<TModel> : EventArgs
    {
        public static NotifyNavigationStackChangedEventArgs<TModel> Create(TModel newHead, TModel oldHead, IEnumerable<TModel> removed)
        {
            Contract.Requires(removed != null);
            return new NotifyNavigationStackChangedEventArgs<TModel>(newHead, oldHead, removed);
        }

        private readonly TModel newHead;
        private readonly TModel oldHead;
        private readonly IEnumerable<TModel> removed;

        private  NotifyNavigationStackChangedEventArgs(TModel newHead, TModel oldHead, IEnumerable<TModel> removed)
        {
            this.newHead = newHead;
            this.oldHead = oldHead;
            this.removed = removed;
        }

        public TModel NewHead
        {
            get
            {
                return newHead;
            }
        }

        public TModel OldHead
        {
            get
            {
                return oldHead;
            }
        }

        public IEnumerable<TModel> Removed
        {
            get
            {
                return removed;
            }
        }
    }

    public interface INavigationStackViewModel<TModel> 
        where TModel: INavigableModel
    {
        event EventHandler<NotifyNavigationStackChangedEventArgs<TModel>> NavigationStackChanged;
        TModel Head { get; }
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

    public interface IModelBinder<TModel> : IInitializable
    {
        IDisposable Bind(TModel model);
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

