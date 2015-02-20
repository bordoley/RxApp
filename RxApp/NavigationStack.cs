using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp
{
    public sealed class NotifyNavigationStackChangedEventArgs : EventArgs
    {
        public static NotifyNavigationStackChangedEventArgs Create(INavigableControllerModel newHead, INavigableControllerModel oldHead, IEnumerable<INavigableControllerModel> removed)
        {
            Contract.Requires(removed != null);
            return new NotifyNavigationStackChangedEventArgs(newHead, oldHead, removed);
        }

        private readonly INavigableControllerModel newHead;
        private readonly INavigableControllerModel oldHead;
        private readonly IEnumerable<INavigableControllerModel> removed;

        private  NotifyNavigationStackChangedEventArgs(INavigableControllerModel newHead, INavigableControllerModel oldHead, IEnumerable<INavigableControllerModel> removed)
        {
            this.newHead = newHead;
            this.oldHead = oldHead;
            this.removed = removed;
        }

        public INavigableControllerModel NewHead
        {
            get
            {
                return newHead;
            }
        }

        public INavigableControllerModel OldHead
        {
            get
            {
                return oldHead;
            }
        }

        public IEnumerable<INavigableControllerModel> Removed
        {
            get
            {
                return removed;
            }
        }
    }
      
    // FIXME FIXME FIME
    // This class needs an overhaul to be made thread safe. 
    // All the update methods 
    //   public void GotoRoot()       
    //   public void Push
    //   public void Pop()
    //   public void SetRoot
    // 
    // Probably use locks, and also use copy semantics for the enumerators
    // so that callers of .GetEnumerator get a snapshot of the current state
    // and don't have to worry about concurrent modification exceptions.
    public static class NavigationStack
    {
        public static INavigationStack Create ()
        {
            return new NavigationStackImpl();
        }
          
        private sealed class NavigationStackImpl : INavigationStack
        {
            private IStack<INavigableControllerModel> navStack = Stack<INavigableControllerModel>.Empty;
            private IDisposable backSubscription = null;

            internal NavigationStackImpl()
            {
            }

            public event EventHandler<NotifyNavigationStackChangedEventArgs> NavigationStackChanged = (o,e) => {};

            public IEnumerator<INavigableControllerModel> GetEnumerator()
            {
                return this.navStack.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)this.navStack).GetEnumerator();
            }

            public void GotoRoot()
            {
                if (!navStack.IsEmpty())
                {
                    var reversed = navStack.Reverse();
                    var oldHead = navStack.Head;
                    var newHead = reversed.Head;
                    var removed = reversed.Tail;

                    if (newHead != oldHead)
                    { 
                        Update(Stack<INavigableControllerModel>.Empty.Push(reversed.Head));
                        NavigationStackChanged(
                            this, 
                            NotifyNavigationStackChangedEventArgs.Create(newHead, oldHead, removed));
                    }
                }              
            }

            public void Push(INavigableControllerModel model)
            {
                Contract.Requires(model != null);

                var oldHead = navStack.Head;
                Update(navStack.Push(model));
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs.Create(model, oldHead, Stack<INavigableControllerModel>.Empty));
            }

            public void Pop()
            {
                var oldHead = navStack.Head;
                Update(navStack.Tail);
                var newHead = navStack.Head;
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs.Create(newHead, oldHead, Stack<INavigableControllerModel>.Empty.Push(oldHead)));
            }

            public void SetRoot(INavigableControllerModel model)
            {
                Contract.Requires(model != null);

                var oldHead = navStack.Head;
                var removed = navStack;

                Update(Stack<INavigableControllerModel>.Empty.Push(model));
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs.Create(model, oldHead, removed));
            }

            private void Update(IStack<INavigableControllerModel> newStack)
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

        public static IDisposable BindController(
            this INavigationStack This,  
            Func<object, IDisposable> provideController)
        {
            Contract.Requires(This != null);
            Contract.Requires(provideController != null);

            var retval = new NavigationStackControllerBinding(This, provideController);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackControllerBinding : IDisposable
        {
            private readonly INavigationStack navStack;
            private readonly Func<object, IDisposable> provideController;
            private readonly IDictionary<object, IDisposable> bindings = new Dictionary<object, IDisposable>();

            private IDisposable navStateChangedSubscription = null;

            internal NavigationStackControllerBinding(
                INavigationStack navStack,  
                Func<object, IDisposable> provideController)
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

                navStateChangedSubscription = RxObservable.FromEventPattern<NotifyNavigationStackChangedEventArgs>(navStack, "NavigationStackChanged").Subscribe(e =>
                    {
                        var head = e.EventArgs.NewHead;
                        var removed = e.EventArgs.Removed;

                        if (head != null && !bindings.ContainsKey(head))
                        {
                            var binding = provideController(head);
                            bindings[head] = binding;
                        }    

                        foreach (var model in removed)
                        {
                            var binding = bindings[model];
                            binding.Dispose();
                            bindings.Remove(model);
                        }
                    });
            }

            public void Dispose()
            {
                navStateChangedSubscription.Dispose();

                foreach (var model in bindings.Keys)
                {
                    var binding = bindings[model];
                    binding.Dispose();
                    bindings.Remove(model);
                }
            }
        }
    }
}