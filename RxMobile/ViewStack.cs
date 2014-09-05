using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace RxMobile
{
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

    public interface IViewStack<TModel> : INotifyPropertyChanged
        where TModel: INavigableModel
    {
        TModel Current { get; }

        void Push(TModel model);
        void SetRoot(TModel model);
    }
        
    public sealed class ViewStack<TModel> : IViewStack<TModel>  
        where TModel: class, INavigableModel
    {
        public static IViewStack<TModel> Create ()
        {
            return new ViewStack<TModel>();
        }

        private static void Close(IEnumerable<INavigableControllerModel> views) 
        {
            SynchronizationContext.Current.Post((d) => {
                foreach (var view in views)
                {
                    view.Close.Execute(null);
                }
            }, null);
        }

        private readonly IReactiveObject notify = ReactiveObjectFactory.Create();
        private IStack<TModel> viewStack = Stack<TModel>.Empty;
        private IDisposable backSubscription = null;

        private ViewStack()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { notify.PropertyChanged += value; }
            remove { notify.PropertyChanged -= value; }
        }

        public TModel Current
        { 
            get
            {
                return viewStack.FirstOrDefault();
            }
        }

        private void GotoRoot()
        {
            var reversed = viewStack.Reverse();
            if (!reversed.IsEmpty())
            {
                Close(reversed.Tail);
                Update(Stack<TModel>.Empty.Push(reversed.Head));
            }              
        }

        public void Push(TModel model)
        {
            // FIXME: Preconditions or Code contract check for null
            Update(viewStack.Push(model));
        }

        private void Pop()
        {
            Close(Stack<TModel>.Empty.Push(viewStack.Head));
            Update(viewStack.Tail);
        }

        public void SetRoot(TModel model)
        {
            // FIXME: Preconditions or Code contract check for null
            Close(viewStack);
            Update(Stack<TModel>.Empty.Push(model));
        }

        private void SubscribeToBack()
        {
            if (backSubscription != null)
            {
                backSubscription.Dispose();
                backSubscription = null;
            }

            var newSubscription = new CompositeDisposable();
            if (!viewStack.IsEmpty())
            {   
                INavigableControllerModel view = viewStack.First();
                newSubscription.Add(view.Back.FirstAsync().Subscribe(_ => this.Pop()));
                newSubscription.Add(view.Up.FirstAsync().Subscribe(_ => this.GotoRoot()));
            }

            backSubscription = newSubscription;
        }

        private void Update(IStack<TModel> newStack)
        {
            viewStack = newStack;
            SubscribeToBack();
            notify.RaisePropertyChanged("Current");
        }
    }
        
    // FIXME: Not positive IController is the right thing here.
    public sealed class ViewStackBinder<TModel> : IController
        where TModel: INavigableModel
    {
        public static IController Create(IViewStack<TModel> viewStack, Action<TModel> presentView, Func<TModel, IController> provideController)
        {
            // FIXME: Preconditions or code contracts
            return new ViewStackBinder<TModel>(viewStack, presentView, provideController);
        }

        private readonly IViewStack<TModel> viewStack;
        private readonly Action<TModel> presentView;
        private readonly Func<TModel, IController> provideController;

        private IDisposable subscription = null;

        private ViewStackBinder(IViewStack<TModel> viewStack, Action<TModel> presentView, Func<TModel, IController> provideController)
        {
            this.viewStack = viewStack;
            this.presentView = presentView;
            this.provideController = provideController;
        }

        public void Initialize()
        {
            if (subscription != null)
            {
                throw new NotSupportedException("Initialize can only be called once");
            }

            IController controller = null;

            subscription =
                viewStack.WhenAnyValue(x => x.Current).Subscribe(next =>
                    {
                        if (controller != null)
                        {
                            controller.Dispose();
                            controller = null;
                        }

                        if (next != null)
                        {
                            presentView(next);
                            controller = provideController(next);
                            controller.Initialize();
                        }
                    });
        }

        public void Dispose()
        {
            subscription.Dispose();
        }
    }
}