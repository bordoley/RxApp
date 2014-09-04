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

    public interface IViewStack : INotifyPropertyChanged
    {
        INavigableModel Current { get; }

        void Push(INavigableModel model);
        void SetRoot(INavigableModel model);
    }

    // FIXME: Don't inherit ReactiveObject, delegate to it instead
    public sealed class ViewStack : ReactiveObject, IViewStack
    {
        public static IViewStack Create ()
        {
            return new ViewStack();
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

        private IStack<INavigableModel> viewStack = Stack<INavigableModel>.Empty;
        private IDisposable backSubscription = null;

        private ViewStack()
        {
        }

        public INavigableModel Current
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
                Update(Stack<INavigableModel>.Empty.Push(reversed.Head));
            }              
        }

        public void Push(INavigableModel model)
        {
            // FIXME: Preconditions or Code contract check for null
            Update(viewStack.Push(model));
        }

        private void Pop()
        {
            Close(Stack<INavigableModel>.Empty.Push(viewStack.Head));
            Update(viewStack.Tail);
        }

        public void SetRoot(INavigableModel model)
        {
            // FIXME: Preconditions or Code contract check for null
            Close(viewStack);
            Update(Stack<INavigableModel>.Empty.Push(model));
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

        private void Update(IStack<INavigableModel> newStack)
        {
            this.RaisePropertyChanging("Current");
            viewStack = newStack;
            SubscribeToBack();
            this.RaisePropertyChanged("Current");
        }
    }

    public sealed class ViewStackBinder : IService
    {
        private readonly IViewStack viewStack;
        private readonly Action<INavigableViewModel> presentView;
        private readonly Func<INavigableControllerModel, IService> provideController;

        private IService controller = null;
        private IDisposable viewModelSubscription = null;

        public static IService Create(IViewStack viewStack, Action<INavigableViewModel> presentView, Func<INavigableControllerModel, IService> provideController)
        {
            // FIXME: Preconditions or code contracts
            return new ViewStackBinder(viewStack, presentView, provideController);
        }

        private ViewStackBinder(IViewStack viewStack, Action<INavigableViewModel> presentView, Func<INavigableControllerModel, IService> provideController)
        {
            this.viewStack = viewStack;
            this.presentView = presentView;
            this.provideController = provideController;
        }

        public void Start()
        {
            if (viewModelSubscription != null)
            {
                throw new NotSupportedException("Trying to call start more than once without first calling stop");
            }

            viewModelSubscription =
                viewStack.WhenAnyValue(x => x.Current).Where(x => x != null).Subscribe(next =>
                    {
                        if (controller != null)
                        {
                            controller.Stop();
                            controller = null;
                        }

                        presentView(next);
                        controller = provideController(next);

                        if (controller != null)
                        {
                            controller.Start();
                        }
                    });
        }

        public void Stop()
        {
            if (viewModelSubscription != null)
            {
                viewModelSubscription.Dispose();
                viewModelSubscription = null;
            }

            if (controller != null)
            {
                controller.Stop();
                controller = null;
            }
        }
    }
}