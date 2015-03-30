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
    public sealed class NavigationApplicationBuilder
    {
        private readonly Dictionary<Type, Func<INavigationControllerModel,IDisposable>> typeToBindFunc = 
            new Dictionary<Type, Func<INavigationControllerModel,IDisposable>>();

        private Func<IDisposable> onConnect = () => RxDisposable.Empty;
        private IObservable<ImmutableStack<INavigationModel>> rootState = null;

        public IObservable<ImmutableStack<INavigationModel>> RootState
        { 
            set
            { 
                Contract.Requires(value != null);
                this.rootState = value; 
            }
        }

        public Func<IDisposable> OnConnect 
        { 
            set 
            { 
                Contract.Requires(value != null);
                this.onConnect = value; 
            } 
        }

        public void RegisterBinding<TModel>(Func<TModel, IDisposable> bind)
            where TModel : INavigationControllerModel
        {
            this.typeToBindFunc.Add(typeof(TModel), model => 
                bind((TModel) model));
        }

        public IConnectableObservable<ImmutableStack<INavigationModel>> Build()
        {
            var typeToBindFunc = this.typeToBindFunc.ToImmutableDictionary();
            var rootState = this.rootState;
            var onConnect = this.onConnect;

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

            return RxObservable.Create<ImmutableStack<INavigationModel>>(observer => 
                {
                    var navigationStackSubject = new Subject<ImmutableStack<INavigationModel>>();
                    var navigationStack = navigationStackSubject.DistinctUntilChanged().Publish();

                    return Disposable.Compose(
                        navigationStack.Connect(),

                        navigationStack.Subscribe(observer),

                        onConnect(),

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
                }).Replay(1);
        }
    }

    internal static class ImmutableStackExt
    {
        internal static ImmutableStack<T> Up<T>(this ImmutableStack<T> This)
        {
            var x = This;
            for (; !x.Pop().IsEmpty; x = x.Pop()) {}
            return x;
        }
    }
}

