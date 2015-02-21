using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp
{
    public sealed class NotifyNavigationStackChangedEventArgs : EventArgs
    {
        public static NotifyNavigationStackChangedEventArgs Create(INavigationModel newHead, INavigationModel oldHead, IEnumerable<INavigationModel> removed)
        {
            Contract.Requires(removed != null);
            return new NotifyNavigationStackChangedEventArgs(newHead, oldHead, removed);
        }

        private readonly INavigationModel newHead;
        private readonly INavigationModel oldHead;
        private readonly IEnumerable<INavigationModel> removed;

        private  NotifyNavigationStackChangedEventArgs(INavigationModel newHead, INavigationModel oldHead, IEnumerable<INavigationModel> removed)
        {
            this.newHead = newHead;
            this.oldHead = oldHead;
            this.removed = removed;
        }

        public INavigationModel NewHead
        {
            get
            {
                return newHead;
            }
        }

        public INavigationModel OldHead
        {
            get
            {
                return oldHead;
            }
        }

        public IEnumerable<INavigationModel> Removed
        {
            get
            {
                return removed;
            }
        }
    }
      
    public static class NavigationStack
    {
        public static INavigationStack Create (IScheduler mainThreadScheduler)
        {
            return new NavigationStackImpl(mainThreadScheduler);
        }
          
        private sealed class NavigationStackImpl : INavigationStack
        {
            private readonly IScheduler mainThreadScheduler;

            private IStack<INavigationModel> navStack = Stack<INavigationModel>.Empty;
            private IDisposable backSubscription = null;

            internal NavigationStackImpl(IScheduler mainThreadScheduler)
            {
                this.mainThreadScheduler = mainThreadScheduler;
            }

            public event EventHandler<NotifyNavigationStackChangedEventArgs> NavigationStackChanged = (o,e) => {};

            public IEnumerator<INavigationModel> GetEnumerator()
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
                        Update(Stack<INavigationModel>.Empty.Push(reversed.Head));
                        NavigationStackChanged(
                            this, 
                            NotifyNavigationStackChangedEventArgs.Create(newHead, oldHead, removed));
                    }
                }              
            }

            public void Push(INavigationModel model)
            {
                Contract.Requires(model != null);

                var oldHead = navStack.Head;
                Update(navStack.Push(model));
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs.Create(model, oldHead, Stack<INavigationModel>.Empty));
            }

            public void Pop()
            {
                var oldHead = navStack.Head;
                Update(navStack.Tail);
                var newHead = navStack.Head;
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs.Create(newHead, oldHead, Stack<INavigationModel>.Empty.Push(oldHead)));
            }

            public void SetRoot(INavigationModel model)
            {
                Contract.Requires(model != null);

                var oldHead = navStack.Head;
                var removed = navStack;

                Update(Stack<INavigationModel>.Empty.Push(model));
                NavigationStackChanged(
                    this, 
                    NotifyNavigationStackChangedEventArgs.Create(model, oldHead, removed));
            }

            private void Update(IStack<INavigationModel> newStack)
            {
                navStack = newStack;

                if (backSubscription != null)
                {
                    backSubscription.Dispose();
                }

                var newSubscription = RxDisposable.Empty;

                if (!navStack.IsEmpty())
                {   
                    INavigationModel view = navStack.First();

                    newSubscription = Disposable.Combine(
                        view.Back.FirstAsync().ObserveOn(mainThreadScheduler).Subscribe(_ => this.Pop()),
                        view.Up.FirstAsync().ObserveOn(mainThreadScheduler).Subscribe(_ => this.GotoRoot())
                    );
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