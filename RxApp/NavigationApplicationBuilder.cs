using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;

using RxDisposable = System.Reactive.Disposables.Disposable;
using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp
{
    // FIXME: Naming issue. Application is a bit of a misnomer. It could just be another window in a desktop app. Maybe NavigableBuilder?
    public sealed class NavigationApplicationBuilder
    {
        private readonly Dictionary<Type, Func<INavigationControllerModel,IDisposable>> typeToBindFunc = 
            new Dictionary<Type, Func<INavigationControllerModel,IDisposable>>();

        private IObservable<NavigationStack> rootState = null;

        public IObservable<NavigationStack> RootState
        { 
            set
            { 
                Contract.Requires(value != null);
                this.rootState = value; 
            }
        }

        public void RegisterBinding<TModel>(Func<TModel, IDisposable> bind)
            where TModel : INavigationControllerModel
        {
            this.typeToBindFunc.Add(typeof(TModel), model => 
                bind((TModel) model));
        }

        public IObservable<NavigationStack> Build()
        {
            var typeToBindFunc = this.typeToBindFunc.ToImmutableDictionary();
            var rootState = this.rootState;

            if (rootState == null) { throw new NotSupportedException("RootState must be set before calling build."); }

            Func<INavigationControllerModel,IDisposable> bind = (INavigationControllerModel model) =>
                {
                    var modelType = model.GetType();
                    foreach (var iface in Enumerable.Concat(new Type[] { modelType }, modelType.GetTypeInfo().ImplementedInterfaces))
                    {
                        Func<INavigationControllerModel,IDisposable> doBind;
                        if (typeToBindFunc.TryGetValue(iface, out doBind))
                        {
                            return doBind(model);
                        }
                    }

                    throw new NotSupportedException("No Controller found for the given model type: " + modelType);
                };

            return RxObservable.Create<NavigationStack>(observer => 
                {
                    var navigationStackSubject = new Subject<NavigationStack>();
                    var navigationStack = navigationStackSubject.DistinctUntilChanged().Publish();

                    return Disposable.Compose(
                        navigationStack.Connect(),

                        navigationStack.Subscribe(observer),

                        navigationStack
                            .Scan(ImmutableDictionary<INavigationModel,IDisposable>.Empty, (acc, stack) =>
                                {
                                    var stackSet = stack.ToImmutableHashSet();
                                    var removed = acc.Keys.Where(x => !stackSet.Contains(x))
                                        .Aggregate(acc, (accA, model) =>
                                            {
                                                accA[model].Dispose();
                                                return accA.Remove(model);
                                            });
                                    return stack.Where(x => !acc.ContainsKey(x))
                                        .Aggregate(removed, (accB, model) => accB.Add(model, bind(model)));
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
                                                stack.Peek().Back.Select(x => stack.Pop()),
                                                stack.Peek().Up.Select(x => stack.Up()),
                                                stack.Peek().Open.Select(x => stack.Push(x)))
                                            .Where(x => x != stack)
                                            .FirstAsync()
                                            .Subscribe(x => navigationStackSubject.OnNext(x));
                                }).Subscribe(),

                        rootState.Subscribe(x => navigationStackSubject.OnNext(x)));
                });
        }
    }

    public sealed class NavigationStack : IReadOnlyCollection<INavigationModel>, IEquatable<NavigationStack>
    {
        public static readonly NavigationStack Empty = 
            new NavigationStack(ImmutableStack<INavigationModel>.Empty, ImmutableHashSet<INavigationModel>.Empty);

        private readonly IImmutableStack<INavigationModel> stack;
        private readonly IImmutableSet<INavigationModel> stackSet;

        private NavigationStack(IImmutableStack<INavigationModel> stack, IImmutableSet<INavigationModel> stackSet)
        {
            this.stack = stack;
            this.stackSet = stackSet;
        }

        public int Count { get { return stackSet.Count; } }

        public bool Contains(INavigationModel model)
        {
            return stackSet.Contains(model);
        }

        public NavigationStack Push(INavigationModel model)
        {
            if (stackSet.Contains(model)) { throw new ArgumentException("Navigation stack already contains the model"); }

            return new NavigationStack(stack.Push(model), stackSet.Add(model));
        }

        public NavigationStack Pop()
        {
            INavigationModel popped;
            var stack = this.stack.Pop(out popped);
            var stackSet = this.stackSet.Remove(popped);

            return new NavigationStack(stack, stackSet);
        }

        public INavigationModel Peek()
        {
            return this.stack.Peek();
        }

        public NavigationStack Up()
        {
            var x = this;
            for (; !x.Pop().IsEmpty; x = x.Pop()) {}
            return x;
        }

        public bool IsEmpty { get { return this.stack.IsEmpty; } }

        public IEnumerator<INavigationModel> GetEnumerator()
        {
            return stack.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool Equals(NavigationStack other)
        {
            if (Object.ReferenceEquals(this, other))      { return true; }
            else if (Object.ReferenceEquals(other, null)) { return false; }
            else if (Object.ReferenceEquals(this, other)) { return true; }
            else                                          { return this.stack.SequenceEqual(other.stack); }
        }

        public override bool Equals(object other)
        {
            return (other is NavigationStack) && (this.Equals((NavigationStack) other));
        }

        public override int GetHashCode()
        {
            return this.stack.Aggregate(0, (acc, model) => acc * 31 + model.GetHashCode()); 
        }
    }
}

