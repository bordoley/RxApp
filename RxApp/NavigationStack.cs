using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxApp
{
    public interface IViewHost<TView>
    {
        void PresentView(TView view);
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

    public static class NavigationStack
    {
        public static IDisposable Bind<TView, TModel, TViewModel, TControllerModel>(
                this INavigationStackViewModel<TModel> navStack, 
                IViewHost<TView> viewHost,
                Func<TViewModel, TView> provideView, 
                Func<TControllerModel, IDisposable> provideController)
            where TView : IDisposable
            where TModel : class, TViewModel, TControllerModel, INavigableModel
            where TViewModel : class, INavigableViewModel
            where TControllerModel : class, INavigableControllerModel
        {
            var retval = new NavigationStackBinding<TView, TModel, TViewModel, TControllerModel>(navStack, viewHost, provideView, provideController);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackBinding<TView, TModel, TViewModel, TControllerModel> : IDisposable
            where TView : IDisposable
            where TModel : class, TViewModel, TControllerModel, INavigableModel
            where TViewModel : class, INavigableViewModel
            where TControllerModel : class, INavigableControllerModel
        {
            private readonly INavigationStackViewModel<TModel> navStack;
            private readonly IViewHost<TView> viewHost;
            private readonly Func<TViewModel,TView> provideView;
            private readonly Func<TControllerModel, IDisposable> provideController;

            private readonly IDictionary<TModel, IDisposable> bindings = new Dictionary<TModel, IDisposable>();

            private IDisposable navStateChangedSubscription = null;

            internal NavigationStackBinding(
                INavigationStackViewModel<TModel> navStack, 
                IViewHost<TView> viewHost,
                Func<TViewModel, TView> provideView, 
                Func<TControllerModel, IDisposable> provideController)
            {
                this.navStack = navStack;
                this.viewHost = viewHost;
                this.provideView = provideView;
                this.provideController = provideController;
            }

            public void Initialize()
            {
                if (navStateChangedSubscription != null)
                {
                    throw new NotSupportedException("Initialize can only be called once");
                }

                // Need to setup the subscription before calling Initialize() to ensure we don't miss any change notifications.
                navStateChangedSubscription = Observable.FromEventPattern<NotifyNavigationStackChangedEventArgs<TModel>>(navStack, "NavigationStackChanged").Subscribe(e =>
                    {
                        var head = e.EventArgs.NewHead;
                        var oldHead = e.EventArgs.OldHead;

                        if (head != null && !bindings.ContainsKey(head))
                        {
                            var controller = provideController(head);
                            var view = provideView(head);

                            viewHost.PresentView(view);

                            var binding = new CompositeDisposable();
                            binding.Add(controller);
                            binding.Add(view);

                            bindings[head] = binding;
                        }    

                        if(oldHead != null && bindings.ContainsKey(oldHead))
                        {
                            var binding = bindings[oldHead];
                            bindings.Remove(oldHead);

                            binding.Dispose();
                        }
                    });
            }

            public void Dispose()
            {
                navStateChangedSubscription.Dispose();

                foreach(var kv in bindings)
                {
                    kv.Value.Dispose();
                }
                bindings.Clear();
            }
        }

        public static INavigationStackModel<TModel> Create<TModel> ()
            where TModel: class, INavigableModel
        {
            return new NavigationStackImpl<TModel>();
        }

        private sealed class NavigationStackImpl<TModel> : INavigationStackModel<TModel> 
            where TModel: class, INavigableModel
        {
            private IStack<TModel> navStack = Stack<TModel>.Empty;
            private IDisposable backSubscription = null;

            internal NavigationStackImpl()
            {
            }

            public event EventHandler<NotifyNavigationStackChangedEventArgs<TModel>> NavigationStackChanged = (o,e) => {};

            public TModel Head
            { 
                get
                {
                    return navStack.FirstOrDefault();
                }
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

            private void Pop()
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