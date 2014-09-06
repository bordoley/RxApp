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
        public static IDisposable Bind<TModel>(this INavigationStackViewModel<TModel> navStack, INavigationViewController navViewController)
            where TModel : INavigableModel
        {
            // FIXME: Preconditions or code contracts
            var retval = new NavigationStackBinding<TModel>(navStack, navViewController);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackBinding<TModel> : IInitializable
            where TModel: INavigableModel
        {
            private readonly INavigationStackViewModel<TModel> navStack;
            private readonly INavigationViewController navViewController;

            private IDisposable subscription = null;

            internal NavigationStackBinding(INavigationStackViewModel<TModel> navStack, INavigationViewController navViewController)
            {
                this.navStack = navStack;
                this.navViewController = navViewController;
            }

            public void Initialize()
            {
                if (subscription != null)
                {
                    throw new NotSupportedException("Initialize can only be called once");
                }

                navViewController.Initialize();

                IDisposable controller = null;

                subscription =
                    navStack.WhenAnyValue(x => x.Current).Subscribe(next =>
                        {
                            if (controller != null)
                            {
                                controller.Dispose();
                                controller = null;
                            }

                            if (next != null)
                            {
                                navViewController.PresentView(next);
                                controller = navViewController.ProvideController(next);
                            }
                        });
            }

            public void Dispose()
            {
                subscription.Dispose();
                navViewController.Dispose();
            }
        }

        public static IDisposableService Bind<TModel>(this INavigationStackViewModel<TModel> navStack, Func<INavigationViewController> controllerProvider)
            where TModel : INavigableModel
        {
            // FIXMe: PReconditions/Code contracts
            return new NavigationStackService<TModel>(navStack, controllerProvider);
        }

        private sealed class NavigationStackService<TModel> : IDisposableService
            where TModel : INavigableModel
        {
            private readonly INavigationStackViewModel<TModel> navStack;
            private readonly Func<INavigationViewController> controllerProvider;

            private IDisposable navStackBinding = null;

            internal NavigationStackService(INavigationStackViewModel<TModel> navStack, Func<INavigationViewController> controllerProvider)
            {
                this.navStack = navStack;
                this.controllerProvider = controllerProvider;
            }

            public void Start()
            {
                if (navStackBinding != null)
                {
                    throw new NotSupportedException("Calling start more than once in a row without first calling stop");
                }

                navStackBinding = navStack.Bind(controllerProvider());
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