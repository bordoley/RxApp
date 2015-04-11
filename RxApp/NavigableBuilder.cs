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
    /// <summary>
    /// A builder for building an virtual application that uses a navigation stack to represent its state. 
    /// The output of the builder is a cold IObservable&lt;NavigationStack&gt; that can be subscribed to
    /// in order to start the application.
    /// </summary>
    public sealed class NavigableBuilder
    {
        private readonly Dictionary<Type, Func<INavigationControllerModel,IDisposable>> typeToBindFunc = 
            new Dictionary<Type, Func<INavigationControllerModel,IDisposable>>();

        private IObservable<NavigationStack> initialState = null;

        /// <summary>
        /// A cold observable that bootstraps the application to an initial state. This observable should
        /// return an initial value on subscription to set the inital state of the navigable's navigation
        /// stack. In addition, this IObservable may reset the navigation stack anytime during the
        /// application's lifecycle. Note, while the Navigable builder itself supports Observables that 
        /// publish an initial state with a depth of more than 1, not all UI connectors do and may not
        /// behave correctly in that rare scenario.
        /// </summary>
        public IObservable<NavigationStack> InitialState
        { 
            set
            { 
                Contract.Requires(value != null);
                this.initialState = value; 
            }
        }

        /// <summary>
        /// Registers a function that can be used to create business logic binding to a model based
        /// upon the model's runtime type.
        /// </summary>
        /// <param name="bind">The function that will create a binding.</param>
        /// <typeparam name="TModel">The model type.</typeparam>
        public void RegisterBinding<TModel>(Func<TModel, IDisposable> bind)
            where TModel : INavigationControllerModel
        {
            this.typeToBindFunc.Add(typeof(TModel), model => 
                bind((TModel) model));
        }

        /// <summary>
        /// Create's a cold observable that when subscribed to starts the application and publishes the current
        /// state of the navigation stack.
        /// </summary>
        public IObservable<NavigationStack> Build()
        {
            var typeToBindFunc = this.typeToBindFunc.ToImmutableDictionary();

            if (this.initialState == null) { throw new NotSupportedException("InitialState must be set before calling build."); }
            var initialState = this.initialState;

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
                                    var removed = acc.Keys.Where(x => !stack.Contains(x))
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

                        initialState.Subscribe(x => navigationStackSubject.OnNext(x)));
                });
        }
    }

    /// <summary>
    /// A persistent immutable stack that is used to represent the current navigation state.
    /// </summary>
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

