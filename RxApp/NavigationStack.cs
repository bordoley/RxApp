using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxApp
{
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

    public interface INavigationStack<TModel> : IEnumerable<TModel>
        where TModel: INavigableModel
    {
        event EventHandler<NotifyNavigationStackChangedEventArgs<TModel>> NavigationStackChanged;
       
        void Pop();
        void Push(TModel model);
        void SetRoot(TModel model);
    }
        
    public static class NavigationStack
    {
        public static IDisposable Bind<TModel, TControllerModel>(
                this INavigationStack<TModel> navStack,  
                Func<TControllerModel, IDisposable> provideController)
            where TModel : class, TControllerModel, INavigableModel
            where TControllerModel : class, INavigableControllerModel
        {
            var retval = new NavigationStackControllerBinding<TModel, TControllerModel>(navStack, provideController);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackControllerBinding<TModel, TControllerModel> : IDisposable
            where TModel : class, TControllerModel, INavigableModel
            where TControllerModel : class, INavigableControllerModel
        {
            private readonly INavigationStack<TModel> navStack;
            private readonly Func<TControllerModel, IDisposable> provideController;

            private IDisposable navStateChangedSubscription = null;

            private IDisposable binding = null;

            internal NavigationStackControllerBinding(
                INavigationStack<TModel> navStack,  
                Func<TControllerModel, IDisposable> provideController)
            {
                this.navStack = navStack;
                this.provideController = provideController;
            }

            public void Initialize()
            {
                if (navStateChangedSubscription != null)
                {
                    throw new NotSupportedException("Initialize can only be called once");
                }

                navStateChangedSubscription = Observable.FromEventPattern<NotifyNavigationStackChangedEventArgs<TModel>>(navStack, "NavigationStackChanged").Subscribe(e =>
                    {
                        var head = e.EventArgs.NewHead;
                        var removed = e.EventArgs.Removed;

                        if (binding != null)
                        {
                            binding.Dispose();
                            binding = null;
                        }

                        if (head != null)
                        {
                            binding = provideController(head);
                        }    
                    });
            }

            public void Dispose()
            {
                navStateChangedSubscription.Dispose();

                if (binding != null)
                {
                    binding.Dispose();
                }
            }
        }

        public static INavigationStack<TModel> Create<TModel> ()
            where TModel: class, INavigableModel
        {
            return new NavigationStackImpl<TModel>();
        }

        private sealed class NavigationStackImpl<TModel> : INavigationStack<TModel> 
            where TModel: class, INavigableModel
        {
            private IStack<TModel> navStack = Stack<TModel>.Empty;
            private IDisposable backSubscription = null;

            internal NavigationStackImpl()
            {
            }

            public event EventHandler<NotifyNavigationStackChangedEventArgs<TModel>> NavigationStackChanged = (o,e) => {};

            public IEnumerator<TModel> GetEnumerator()
            {
                return this.navStack.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)this.navStack).GetEnumerator();
            }

            private void GotoRoot()
            {
                if (!navStack.IsEmpty())
                {
                    var reversed = navStack.Reverse();
                    var oldHead = navStack.Head;
                    var newHead = reversed.Head;
                    var removed = reversed.Tail;

                    if (newHead != oldHead)
                    { 
                        Update(Stack<TModel>.Empty.Push(reversed.Head));
                        NavigationStackChanged(
                            this, 
                            NotifyNavigationStackChangedEventArgs<TModel>.Create(newHead, oldHead, removed));
                    }
                }              
            }

            public void Push(TModel model)
            {
                Contract.Requires(model != null);

                var oldHead = navStack.Head;
                Update(navStack.Push(model));
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs<TModel>.Create(model, oldHead, Stack<TModel>.Empty));
            }

            public void Pop()
            {
                var oldHead = navStack.Head;
                Update(navStack.Tail);
                var newHead = navStack.Head;
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs<TModel>.Create(newHead, oldHead, Stack<TModel>.Empty.Push(oldHead)));
            }

            public void SetRoot(TModel model)
            {
                Contract.Requires(model != null);

                var oldHead = navStack.Head;
                var removed = navStack;

                Update(Stack<TModel>.Empty.Push(model));
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs<TModel>.Create(model, oldHead, removed));
            }

            private void Update(IStack<TModel> newStack)
            {
                navStack = newStack;

                if (backSubscription != null)
                {
                    backSubscription.Dispose();
                    backSubscription = null;
                }

                var newSubscription = new CompositeDisposable();
                if (!navStack.IsEmpty())
                {   
                    INavigableControllerModel view = navStack.First();
                    newSubscription.Add(view.Back.FirstAsync().Subscribe(_ => this.Pop()));
                    newSubscription.Add(view.Up.FirstAsync().Subscribe(_ => this.GotoRoot()));
                }

                backSubscription = newSubscription;
            }
        }
    }
}