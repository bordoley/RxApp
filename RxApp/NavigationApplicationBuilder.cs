﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;
using System.Threading;

namespace RxApp
{
    public sealed class NavigationApplicationBuilder
    {
        private readonly Dictionary<Type, Func<INavigationControllerModel,IDisposable>> typeToControllerProvider = 
            new Dictionary<Type, Func<INavigationControllerModel,IDisposable>>();

        public IObservable<INavigationModel> RootState { get; set; }

        public void RegisterControllerProvider<TModel> (Func<TModel, IDisposable> controllerProvider)
            where TModel : INavigationControllerModel
        {
            this.typeToControllerProvider.Add(typeof(TModel), model => 
                controllerProvider((TModel) model));
        }

        public IConnectableObservable<IEnumerable<INavigationModel>> Build()
        {
            var typeToControllerProvider = this.typeToControllerProvider.ToDictionary(entry => entry.Key, entry => entry.Value);
            var rootState = this.RootState;

            Func<INavigationControllerModel,IDisposable> bind = (INavigationControllerModel model) =>
                {
                    var modelType = model.GetType();
                    foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
                    {
                        Func<INavigationControllerModel,IDisposable> controllerProvider;
                        if (typeToControllerProvider.TryGetValue(iface, out controllerProvider))
                        {
                            return controllerProvider(model);
                        }
                    }

                    throw new NotSupportedException("No Controller found for the given model type: " + modelType);
                };

            return RxObservable.Create<IEnumerable<INavigationModel>>(observer => 
                {
                    var navigationStackSubject = new Subject<Stack<INavigationModel>>();
                    var navigationStack = navigationStackSubject.DistinctUntilChanged().Publish();

                    return Disposable.Compose(
                        navigationStack.Connect(),

                        navigationStack.Subscribe(observer),

                        navigationStack
                            // Since we use a mutable dictionary as the accumulator, be extra paranoid
                            // and synchronize the observable.
                            .Synchronize()
                            .Scan(new Dictionary<INavigationModel,IDisposable>(), (acc, stack) =>
                                {
                                    var head = stack.Head;
                                    var stackSet = new HashSet<INavigationModel>(stack);

                                    // Need to copy the keys to an array to avoid a concurrent modification exception
                                    foreach (var key in acc.Keys.Where(x => !stackSet.Contains(x)).ToArray())
                                    {
                                        acc[key].Dispose();
                                        acc.Remove(key);
                                    }

                                    if (head != null && !acc.ContainsKey(head))
                                    {
                                        acc.Add(head, bind(head));
                                    }

                                    return acc;
                                })
                            .Subscribe(),
                        
                        navigationStack
                            .Scan(RxDisposable.Empty, (acc, stack) =>
                            {
                                acc.Dispose();

                                return stack.IsEmpty ?
                                    RxDisposable.Empty :
                                    RxObservable
                                        .Merge(
                                            stack.Head.Back.Select(x => stack.Tail),
                                            stack.Head.Up.Select(x => stack.Up()),
                                            stack.Head.Open.Select(x => stack.Push(x)))
                                        .Where(x => x != stack)
                                        .FirstAsync()
                                        .Subscribe(x => navigationStackSubject.OnNext(x));
                            }).Subscribe(),

                        rootState
                            .Select(x => Stack<INavigationModel>.Empty.Push(x))
                            .Subscribe(x => navigationStackSubject.OnNext(x)));
                }).Replay(1);
        }

        // A trivial cons list implementation
        // FIXME: Might need to implement equality
        private sealed class Stack<T> : IEnumerable<T>
        {
            private static readonly Stack<T> empty = new Stack<T>(default(T), null);

            public static Stack<T> Empty { get { return empty; } }

            private static IEnumerator<T> Enumerate(Stack<T> stack)
            {
                for (;stack.Head != null; stack = stack.Tail)
                {
                    yield return stack.Head;
                }
            }

            private readonly Stack<T> tail;
            private readonly T head;

            internal Stack(T head, Stack<T> tail)
            {
                this.head = head;
                this.tail = tail;
            }

            public bool IsEmpty { get { return (this.Head == null) && (this.Tail == null); } }

            public T Head { get { return head; } }

            public Stack<T> Tail  {  get { return tail; } }

            public Stack<T> Push(T element)
            {
                return new Stack<T>(element, this);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return Enumerate(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public Stack<T> Up()
            {
                var x = this;
                for (; !x.Tail.IsEmpty; x = x.Tail) {}
                return x;
            }
        }
    }
}

