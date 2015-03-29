using System;
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

        public IObservable<IEnumerable<INavigationModel>> Build()
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
                    var navigationStack = new Subject<Stack<INavigationModel>>();

                    return Disposable.Compose(
                        navigationStack.DistinctUntilChanged().Subscribe(observer),

                        navigationStack
                            .DistinctUntilChanged()
                            .Scan(new Dictionary<INavigationModel,IDisposable>(), (acc, stack) =>
                                {
                                    var head = stack.Head;
                                    var stackSet = new HashSet<INavigationModel>(stack);

                                    foreach (var key in acc.Keys.Where(x => !stackSet.Contains(x)))
                                    {
                                        acc[key].Dispose();
                                    }

                                    // FIXME: An immutablish dictionary would be preferable,
                                    // This method can get called from multiple threads,
                                    // so thread safety is needed.
                                    var newAcc = acc.ToDictionary(x => x.Key, x => x.Value);

                                    if (head != null && !newAcc.ContainsKey(head))
                                    {
                                        newAcc.Add(head, bind(head));
                                    }

                                    return newAcc;
                                })
                            .Subscribe(),
                        
                        navigationStack
                            .DistinctUntilChanged()
                            .Scan(RxDisposable.Empty, (acc, stack) =>
                            {
                                acc.Dispose();

                                if (stack.IsEmpty)
                                {
                                    return RxDisposable.Empty;
                                }
                                else 
                                {
                                    return RxObservable
                                        .Merge(
                                            stack.Head.Back.Select(x => stack.Tail),
                                            stack.Head.Up.Select(x => stack.Up()),
                                            stack.Head.Open.Select(x => stack.Push(x)))
                                        .Where(x => x != stack)
                                        .FirstAsync()
                                        .Subscribe(x => navigationStack.OnNext(x));
                                }
                            }).Subscribe(),

                        rootState
                            .Select(x => Stack<INavigationModel>.Empty.Push(x))
                            .Subscribe(x => navigationStack.OnNext(x)));
                });
        }

        // A trivial cons list implementation
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

