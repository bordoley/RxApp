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
        public static IInitializable Bind<TModel>(this IModelStack<TModel> modelStack, IViewPresenter viewPresenter, IControllerProvider controllerProvider)
            where TModel : INavigableModel
        {
            // FIXME: Preconditions or code contracts
            return new ViewStackBinder<TModel>(modelStack, viewPresenter, controllerProvider);
        }

        public static IModelStack<TModel> Create<TModel> ()
            where TModel: class, INavigableModel
        {
            return new ViewStackImpl<TModel>();
        }

        private sealed class ViewStackBinder<TModel> : IInitializable
            where TModel: INavigableModel
        {
            private readonly IModelStack<TModel> modelStack;
            private readonly IViewPresenter viewPresenter;
            private readonly IControllerProvider controllerProvider;

            private IDisposable subscription = null;

            internal ViewStackBinder(IModelStack<TModel> modelStack, IViewPresenter viewPresenter, IControllerProvider controllerProvider)
            {
                this.modelStack = modelStack;
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
                    modelStack.WhenAnyValue(x => x.Current).Subscribe(next =>
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
            private IStack<TModel> modelStack = Stack<TModel>.Empty;
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
                    return modelStack.FirstOrDefault();
                }
            }

            private void GotoRoot()
            {
                var reversed = modelStack.Reverse();
                if (!reversed.IsEmpty())
                {
                    Close(reversed.Tail);
                    Update(Stack<TModel>.Empty.Push(reversed.Head));
                }              
            }

            public void Push(TModel model)
            {
                // FIXME: Preconditions or Code contract check for null
                Update(modelStack.Push(model));
            }

            private void Pop()
            {
                Close(Stack<TModel>.Empty.Push(modelStack.Head));
                Update(modelStack.Tail);
            }

            public void SetRoot(TModel model)
            {
                // FIXME: Preconditions or Code contract check for null
                Close(modelStack);
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
                if (!modelStack.IsEmpty())
                {   
                    INavigableControllerModel view = modelStack.First();
                    newSubscription.Add(view.Back.FirstAsync().Subscribe(_ => this.Pop()));
                    newSubscription.Add(view.Up.FirstAsync().Subscribe(_ => this.GotoRoot()));
                }

                backSubscription = newSubscription;
            }

            private void Update(IStack<TModel> newStack)
            {
                modelStack = newStack;
                SubscribeToBack();
                notify.RaisePropertyChanged("Current");
            }
        }
    }
}