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
    internal sealed class NotifyNavigationStackChangedEventArgs<T> : EventArgs
        where T: INavigableModel<T>
    {
        public static NotifyNavigationStackChangedEventArgs<T> Create(T newHead, T oldHead, IEnumerable<T> removed)
        {
            Contract.Requires(removed != null);
            return new NotifyNavigationStackChangedEventArgs<T>(newHead, oldHead, removed);
        }

        private readonly T newHead;
        private readonly T oldHead;
        private readonly IEnumerable<T> removed;

        private  NotifyNavigationStackChangedEventArgs(T newHead, T oldHead, IEnumerable<T> removed)
        {
            this.newHead = newHead;
            this.oldHead = oldHead;
            this.removed = removed;
        }

        public T NewHead
        {
            get
            {
                return newHead;
            }
        }

        public T OldHead
        {
            get
            {
                return oldHead;
            }
        }

        public IEnumerable<T> Removed
        {
            get
            {
                return removed;
            }
        }
    }
      
    internal sealed class NavigationStack<T> : IEnumerable<T>
        where T: class, INavigableModel<T>
    {
        public static NavigationStack<T> Create (IScheduler mainThreadScheduler)
        {
            Contract.Requires(mainThreadScheduler != null);

            return new NavigationStack<T>(mainThreadScheduler);
        }

        private readonly IScheduler mainThreadScheduler;

        private IStack<T> navStack = Stack<T>.Empty;
        private IDisposable subscription = null;

        private NavigationStack(IScheduler mainThreadScheduler)
        {
            this.mainThreadScheduler = mainThreadScheduler;
        }

        public event EventHandler<NotifyNavigationStackChangedEventArgs<T>> NavigationStackChanged = (o,e) => {};

        public IEnumerator<T> GetEnumerator()
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
                    Update(Stack<T>.Empty.Push(reversed.Head));
                    NavigationStackChanged(
                        this, 
                        NotifyNavigationStackChangedEventArgs<T>.Create(newHead, oldHead, removed));
                }
            }              
        }

        private void Push(T model)
        {
            Contract.Requires(model != null);

            var oldHead = navStack.Head;
            Update(navStack.Push(model));
            NavigationStackChanged(
                this, 
                NotifyNavigationStackChangedEventArgs<T>.Create(model, oldHead, Stack<T>.Empty));
        }

        private void Pop()
        {
            var oldHead = navStack.Head;
            Update(navStack.Tail);
            var newHead = navStack.Head;
            NavigationStackChanged(
                this, 
                NotifyNavigationStackChangedEventArgs<T>.Create(newHead, oldHead, Stack<T>.Empty.Push(oldHead)));
        }

        public void SetRoot(T model)
        {
            Contract.Requires(model != null);

            var oldHead = navStack.Head;
            var removed = navStack;

            Update(Stack<T>.Empty.Push(model));
            NavigationStackChanged(
                this, 
                NotifyNavigationStackChangedEventArgs<T>.Create(model, oldHead, removed));
        }

        private void Update(IStack<T> newStack)
        {
            navStack = newStack;

            if (subscription != null)
            {
                subscription.Dispose();
            }

            var newSubscription = RxDisposable.Empty;

            if (!navStack.IsEmpty())
            {   
                T model = navStack.First();

                newSubscription = Disposable.Compose(
                    model.Back.FirstAsync().ObserveOn(mainThreadScheduler).Subscribe(_ => this.Pop()),
                    model.Up.FirstAsync().ObserveOn(mainThreadScheduler).Subscribe(_ => this.GotoRoot()),
                    model.Open.FirstAsync().ObserveOn(mainThreadScheduler).Subscribe(x => this.Push(x))
                );
            }

            subscription = newSubscription;
        }
    }

    internal static class NavigationStackExtensions 
    {
        public static IDisposable BindTo<T>(
                this NavigationStack<T> This,  
                Func<T, IDisposable> createBinding)
            where T : class, INavigableModel<T>
        {
            Contract.Requires(This != null);
            Contract.Requires(createBinding != null);

            var retval = new NavigationStackBinding<T>(This, createBinding);
            retval.Initialize();
            return retval;
        }

        private sealed class NavigationStackBinding<T> : IDisposable
            where T : class, INavigableModel<T>
        {
            private readonly NavigationStack<T> navStack;
            private readonly Func<T, IDisposable> createBinding;
            private readonly IDictionary<object, IDisposable> bindings = new Dictionary<object, IDisposable>();

            private IDisposable navStateChangedSubscription = null;

            internal NavigationStackBinding(
                NavigationStack<T> navStack,  
                Func<T, IDisposable> provideController)
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

                navStateChangedSubscription = RxObservable.FromEventPattern<NotifyNavigationStackChangedEventArgs<T>>(navStack, "NavigationStackChanged").Subscribe(e =>
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