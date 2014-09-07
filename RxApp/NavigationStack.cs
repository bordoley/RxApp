using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace RxApp
{
    public static class NavigationStack
    {
        public static IDisposable Bind<TModel>(this INavigationStackViewModel<TModel> navStack, IModelBinder<TModel> binder)
            where TModel : INavigableModel
        {
            // FIXME: Preconditions or code contracts
            var retval = new NavigationStackBinding<TModel>(navStack, binder);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackBinding<TModel> : IInitializable
            where TModel: INavigableModel
        {
            private readonly INavigationStackViewModel<TModel> navStack;
            private readonly IModelBinder<TModel> binder;

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

                binder.Initialize();

                IDisposable binding = null;

                // FIXME: Add an onComplete listener so that when the subscription is Disposed
                // the binding is also is disposed in not null
                subscription =
                    navStack.WhenAnyValue(x => x.Current).Subscribe(next =>
                        {
                            if (binding  != null)
                            {
                                binding.Dispose();
                                binding = null;
                            }

                            if (next != null)
                            {
                                binding = binder.Bind(next);
                            }
                        });
            }

            public void Dispose()
            {
                subscription.Dispose();
                binder.Dispose();
            }
        }

        public static IDisposableService Bind<TModel>(this INavigationStackViewModel<TModel> navStack, Func<IModelBinder<TModel>> binderProvider)
            where TModel : INavigableModel
        {
            // FIXMe: PReconditions/Code contracts
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
                    navStackBinding = null;
                }
            }
        }

        public static INavigationStackModel<TModel> Create<TModel> ()
            where TModel: class, INavigableModel
        {
            return new ViewStackImpl<TModel>();
        }

        private sealed class ViewStackImpl<TModel> : INavigationStackModel<TModel> 
            where TModel: class, INavigableModel
        {
            private static void Close(IEnumerable<INavigableControllerModel> views) 
            {
                SynchronizationContext.Current.Post((d) => {
                    foreach (var view in views)
                    {
                        view.Close.Execute(null);
                    }
                }, null);
            }

            private readonly IReactiveObject notify = ReactiveObject.Create();
            private IStack<TModel> navStack = Stack<TModel>.Empty;
            private IDisposable backSubscription = null;

            internal ViewStackImpl()
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
                    return navStack.FirstOrDefault();
                }
            }

            private void GotoRoot()
            {
                var reversed = navStack.Reverse();
                if (!reversed.IsEmpty())
                {
                    Close(reversed.Tail);
                    Update(Stack<TModel>.Empty.Push(reversed.Head));
                }              
            }

            public void Push(TModel model)
            {
                // FIXME: Preconditions or Code contract check for null
                Update(navStack.Push(model));
            }

            private void Pop()
            {
                Close(Stack<TModel>.Empty.Push(navStack.Head));
                Update(navStack.Tail);
            }

            public void SetRoot(TModel model)
            {
                // FIXME: Preconditions or Code contract check for null
                Close(navStack);
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
                if (!navStack.IsEmpty())
                {   
                    INavigableControllerModel view = navStack.First();
                    newSubscription.Add(view.Back.FirstAsync().Subscribe(_ => this.Pop()));
                    newSubscription.Add(view.Up.FirstAsync().Subscribe(_ => this.GotoRoot()));
                }

                backSubscription = newSubscription;
            }

            private void Update(IStack<TModel> newStack)
            {
                navStack = newStack;
                SubscribeToBack();
                notify.RaisePropertyChanged("Current");
            }
        }
    }
}