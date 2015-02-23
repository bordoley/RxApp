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
    internal sealed class NotifyNavigationStackChangedEventArgs : EventArgs
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
      
    internal sealed class NavigationStack : IEnumerable<INavigationModel>
    {
        public static NavigationStack Create (IScheduler mainThreadScheduler)
        {
            Contract.Requires(mainThreadScheduler != null);

            return new NavigationStack(mainThreadScheduler);
        }

        private readonly IScheduler mainThreadScheduler;

        private IStack<INavigationModel> navStack = Stack<INavigationModel>.Empty;
        private IDisposable subscription = null;

        private NavigationStack(IScheduler mainThreadScheduler)
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
                    Update(Stack<INavigationModel>.Empty.Push(reversed.Head));
                    NavigationStackChanged(
                        this, 
                        NotifyNavigationStackChangedEventArgs.Create(newHead, oldHead, removed));
                }
            }              
        }

        private void Push(INavigationModel model)
        {
            Contract.Requires(model != null);

            var oldHead = navStack.Head;
            Update(navStack.Push(model));
            NavigationStackChanged(
                this, 
                NotifyNavigationStackChangedEventArgs.Create(model, oldHead, Stack<INavigationModel>.Empty));
        }

        private void Pop()
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

            if (subscription != null)
            {
                subscription.Dispose();
            }

            var newSubscription = RxDisposable.Empty;

            if (!navStack.IsEmpty())
            {   
                INavigationModel view = navStack.First();

                newSubscription = Disposable.Compose(
                    view.Back.FirstAsync().ObserveOn(mainThreadScheduler).Subscribe(_ => this.Pop()),
                    view.Up.FirstAsync().ObserveOn(mainThreadScheduler).Subscribe(_ => this.GotoRoot()),
                    view.Open.FirstAsync().ObserveOn(mainThreadScheduler).Subscribe(x => this.Push(x))
                );
            }

            subscription = newSubscription;
        }
    }

    internal static class NavigationStackExtensions 
    {
        public static IDisposable BindTo(
            this NavigationStack This,  
            Func<object, IDisposable> createBinding)
        {
            Contract.Requires(This != null);
            Contract.Requires(createBinding != null);

            var retval = new NavigationStackBinding(This, createBinding);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackBinding : IDisposable
        {
            private readonly NavigationStack navStack;
            private readonly Func<object, IDisposable> createBinding;
            private readonly IDictionary<object, IDisposable> bindings = new Dictionary<object, IDisposable>();

            private IDisposable navStateChangedSubscription = null;

            internal NavigationStackBinding(
                NavigationStack navStack,  
                Func<object, IDisposable> provideController)
            {
                this.navStack = navStack;
                this.createBinding = provideController;
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
                            var binding = createBinding(head);
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