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
    public static class NavigationStack
    {
        internal static IDisposable Bind<TModel>(this INavigationStackViewModel<TModel> navStack, IModelBinder<TModel> binder)
            where TModel : INavigableModel
        {
            var retval = new NavigationStackBinding<TModel>(navStack, binder);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackBinding<TModel> : IInitializable
            where TModel: INavigableModel
        {
            private readonly INavigationStackViewModel<TModel> navStack;
            private readonly IModelBinder<TModel> binder;

            private readonly IDictionary<TModel, IDisposable> bindings = new Dictionary<TModel, IDisposable>();
            private IDisposable subscription = null;

            internal NavigationStackBinding(INavigationStackViewModel<TModel> navStack, IModelBinder<TModel> binder)
            {
                this.navStack = navStack;
                this.binder = binder;
            }

            public void Initialize()
            {
                if (subscription != null)
                {
                    throw new NotSupportedException("Initialize can only be called once");
                }

                // Need to setup the subscription before calling Initialize() to ensure we don't miss any change notifications.
                subscription = Observable.FromEventPattern<NotifyNavigationStackChangedEventArgs<TModel>>(navStack, "NavigationStackChanged").Subscribe(e =>
                    {
                        var head = e.EventArgs.NewHead;
                        var removed = e.EventArgs.Removed;

                        if (head != null && !bindings.ContainsKey(head))
                        {
                            var binding = binder.Bind(head);
                            bindings[head] = binding;
                        }    

                        foreach (var model in removed)
                        {
                            IDisposable binding = null;
                            if (bindings.TryGetValue(model, out binding))
                            {
                                binding.Dispose();
                            }
                        }
                    });

                binder.Initialize();
            }

            public void Dispose()
            {
                binder.Dispose();
                subscription.Dispose();

                foreach(var kv in bindings)
                {
                    kv.Value.Dispose();
                }
                bindings.Clear();
            }
        }

        public static IDisposableService Bind<TModel>(this INavigationStackViewModel<TModel> navStack, Func<IModelBinder<TModel>> binderProvider)
            where TModel : INavigableModel
        {
            Contract.Requires(navStack != null);
            Contract.Requires(binderProvider != null);

            return new NavigationStackService<TModel>(navStack, binderProvider);
        }

        private sealed class NavigationStackService<TModel> : IDisposableService
            where TModel : INavigableModel
        {
            private readonly INavigationStackViewModel<TModel> navStack;
            private readonly Func<IModelBinder<TModel>> binderProvider;

            private IDisposable navStackBinding = null;

            internal NavigationStackService(INavigationStackViewModel<TModel> navStack, Func<IModelBinder<TModel>> binderProvider)
            {
                this.navStack = navStack;
                this.binderProvider = binderProvider;
            }

            public void Start()
            {
                if (navStackBinding != null)
                {
                    throw new NotSupportedException("Calling start more than once in a row without first calling stop");
                }

                navStackBinding = navStack.Bind(binderProvider());
            }

            public void Stop()
            {
                navStackBinding.Dispose();
                navStackBinding = null;
            }

            public void Dispose()
            {
                if (navStackBinding != null)
                {
                    navStackBinding.Dispose();
                }
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

                    Update(Stack<TModel>.Empty.Push(reversed.Head));
                    NavigationStackChanged(
                        this, 
                        NotifyNavigationStackChangedEventArgs<TModel>.Create(newHead, oldHead, removed));
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