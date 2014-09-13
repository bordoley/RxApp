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
        internal static IDisposable Bind<TModel, TViewModel, TControllerModel>(
                this INavigationStackViewModel<TModel> navStack, 
                IViewHost viewHost,
                IViewModelBinder<TViewModel> viewModelBinder, 
                IControllerModelBinder<TControllerModel> controllerModelBinder) 
            where TModel : class, TViewModel, TControllerModel, INavigableModel
            where TViewModel : class, INavigableViewModel
            where TControllerModel : class, INavigableControllerModel
        {
            var retval = new NavigationStackBinding<TModel, TViewModel, TControllerModel>(navStack, viewHost, viewModelBinder, controllerModelBinder);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackBinding<TModel, TViewModel, TControllerModel> : IInitializable
            where TModel : class, TViewModel, TControllerModel, INavigableModel
            where TViewModel : class, INavigableViewModel
            where TControllerModel : class, INavigableControllerModel
        {
            private readonly INavigationStackViewModel<TModel> navStack;
            private readonly IViewHost viewHost;
            private readonly IViewModelBinder<TViewModel> viewModelBinder;
            private readonly IControllerModelBinder<TControllerModel> controllerModelBinder; 
            private readonly IDictionary<TModel, IDisposable> bindings = new Dictionary<TModel, IDisposable>();
            private IDisposable navStateChangedSubscription = null;

            internal NavigationStackBinding(
                INavigationStackViewModel<TModel> navStack, 
                IViewHost viewHost,
                IViewModelBinder<TViewModel> viewModelBinder, 
                IControllerModelBinder<TControllerModel> controllerModelBinder)
            {
                this.navStack = navStack;
                this.viewHost = viewHost;
                this.viewModelBinder = viewModelBinder;
                this.controllerModelBinder = controllerModelBinder;
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
                            var controller = controllerModelBinder.Bind(head);
                            var view = viewModelBinder.Bind(head);
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

                viewModelBinder.Initialize();
                controllerModelBinder.Initialize();
            }

            public void Dispose()
            {
                controllerModelBinder.Dispose();
                viewModelBinder.Dispose();
                navStateChangedSubscription.Dispose();

                foreach(var kv in bindings)
                {
                    kv.Value.Dispose();
                }
                bindings.Clear();
            }
        }

        public static IDisposableService Bind<TModel, TViewModel, TControllerModel>(
                this INavigationStackViewModel<TModel> navStack, 
                IViewHost viewHost,
                Func<IViewModelBinder<TViewModel>> viewModelBinderProvider, 
                Func<IControllerModelBinder<TControllerModel>> controllerModelBinderProvider)
            where TModel : class, TViewModel, TControllerModel, INavigableModel
            where TViewModel : class, INavigableViewModel
            where TControllerModel : class, INavigableControllerModel
        {
            Contract.Requires(navStack != null);
            Contract.Requires(viewHost != null);
            Contract.Requires(viewModelBinderProvider != null);
            Contract.Requires(controllerModelBinderProvider != null);

            return new NavigationStackService<TModel, TViewModel, TControllerModel>(navStack, viewHost, viewModelBinderProvider, controllerModelBinderProvider);
        }

        private sealed class NavigationStackService<TModel, TViewModel, TControllerModel> : IDisposableService
            where TModel : class, TViewModel, TControllerModel, INavigableModel
            where TViewModel : class, INavigableViewModel
            where TControllerModel : class, INavigableControllerModel
        {
            private readonly INavigationStackViewModel<TModel> navStack;
            private readonly IViewHost viewHost;
            private readonly Func<IViewModelBinder<TViewModel>> viewModelBinderProvider;
            private readonly Func<IControllerModelBinder<TControllerModel>> controllerModelBinderProvider;

            private IDisposable navStackBinding = null;

            internal NavigationStackService(
                INavigationStackViewModel<TModel> navStack, 
                IViewHost viewHost,
                Func<IViewModelBinder<TViewModel>> viewModelBinderProvider, 
                Func<IControllerModelBinder<TControllerModel>> controllerModelBinderProvider)
            {
                this.navStack = navStack;
                this.viewHost = viewHost;
                this.viewModelBinderProvider = viewModelBinderProvider;
                this.controllerModelBinderProvider = controllerModelBinderProvider;
            }

            public void Start()
            {
                if (navStackBinding != null)
                {
                    throw new NotSupportedException("Calling start more than once in a row without first calling stop");
                }

                navStackBinding = navStack.Bind<TModel, TViewModel, TControllerModel>(viewHost, viewModelBinderProvider(), controllerModelBinderProvider());
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