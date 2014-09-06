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
    public static class ModelStack
    {
        public static IInitable Bind<TModel>(this IModelStack<TModel> viewStack, IViewPresenter viewPresenter, IControllerProvider controllerProvider)
            where TModel : INavigableModel
        {
            // FIXME: Preconditions or code contracts
            return new ViewStackBinder<TModel>(viewStack, viewPresenter, controllerProvider);
        }

        public static IModelStack<TModel> Create<TModel> ()
            where TModel: class, INavigableModel
        {
            return new ViewStackImpl<TModel>();
        }

        private sealed class ViewStackBinder<TModel> : IInitable
            where TModel: INavigableModel
        {
            private readonly IModelStack<TModel> viewStack;
            private readonly IViewPresenter viewPresenter;
            private readonly IControllerProvider controllerProvider;

            private IDisposable subscription = null;

            internal ViewStackBinder(IModelStack<TModel> viewStack, IViewPresenter viewPresenter, IControllerProvider controllerProvider)
            {
                this.viewStack = viewStack;
                this.viewPresenter = viewPresenter;
                this.controllerProvider = controllerProvider;
            }

            public void Init()
            {
                if (subscription != null)
                {
                    throw new NotSupportedException("Initialize can only be called once");
                }

                viewPresenter.Init();
                controllerProvider.Init();

                IInitable controller = null;

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
                                viewPresenter.PresentView(next);
                                controller = controllerProvider.ProvideController(next);
                                controller.Init();
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

        private sealed class ViewStackImpl<TModel> : IModelStack<TModel> 
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
            private IStack<TModel> viewStack = Stack<TModel>.Empty;
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
            

    }
}