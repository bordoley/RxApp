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
        public static IInitializable Bind<TModel>(this INavigationStack<TModel> navStack, IViewPresenter viewPresenter, IControllerProvider controllerProvider)
            where TModel : INavigableModel
        {
            // FIXME: Preconditions or code contracts
            return new ViewStackBinder<TModel>(navStack, viewPresenter, controllerProvider);
        }

        public static INavigationStack<TModel> Create<TModel> ()
            where TModel: class, INavigableModel
        {
            return new ViewStackImpl<TModel>();
        }

        private sealed class ViewStackBinder<TModel> : IInitializable
            where TModel: INavigableModel
        {
            private readonly INavigationStack<TModel> navStack;
            private readonly IViewPresenter viewPresenter;
            private readonly IControllerProvider controllerProvider;

            private IDisposable subscription = null;

            internal ViewStackBinder(INavigationStack<TModel> navStack, IViewPresenter viewPresenter, IControllerProvider controllerProvider)
            {
                this.navStack = navStack;
                this.viewPresenter = viewPresenter;
                this.controllerProvider = controllerProvider;
            }

            public void Initialize()
            {
                if (subscription != null)
                {
                    throw new NotSupportedException("Initialize can only be called once");
                }

                viewPresenter.Initialize();
                controllerProvider.Initialize();

                IInitializable controller = null;

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
                                viewPresenter.PresentView(next);
                                controller = controllerProvider.ProvideController(next);
                                controller.Initialize();
                            }
                        });
            }

            public void Dispose()
            {
                subscription.Dispose();
                viewPresenter.Dispose();
                controllerProvider.Dispose();
            }
        }

        private sealed class ViewStackImpl<TModel> : INavigationStack<TModel> 
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