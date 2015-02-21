using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

using RxObservable = System.Reactive.Linq.Observable;

namespace RxApp
{
    public interface IRxCommand : IObservable<Unit>
    {
        IObservable<bool> CanExecute { get; }

        void Execute();
    }

    public interface IRxCommand<T> : IObservable<T>
    {
        IObservable<bool> CanExecute { get; }

        void Execute(T parameter);
    }

    public static class RxCommand
    {
        public static IRxCommand Create() 
        {
            var deleg = new RxCommandImpl<Unit>(RxObservable.Return(true));
            return new RxCommandImpl(deleg);
        }

        public static IRxCommand<T> Create<T>() 
        {
            return new RxCommandImpl<T>(RxObservable.Return(true));
        }

        public static IRxCommand ToCommand(this IObservable<bool> This)
        {
            var deleg = new RxCommandImpl<Unit>(This);
            return new RxCommandImpl(deleg);
        }

        public static IRxCommand<T> ToCommand<T>(this IObservable<bool> This)
        {
            return new RxCommandImpl<T>(This);
        }

        public static IDisposable InvokeCommand<T>(this IObservable<T> This, IRxCommand command)
        {
            return This.Subscribe(x => command.Execute());
        }

        public static IDisposable InvokeCommand<T>(this IObservable<T> This, IRxCommand<T> command)
        {
            return This.Subscribe(x => command.Execute(x));
        }

        private sealed class RxCommandImpl : IRxCommand
        {
            private readonly IRxCommand<Unit> deleg;

            internal RxCommandImpl(IRxCommand<Unit> deleg)
            {
                this.deleg = deleg;
            }

            public void Execute()
            {
                deleg.Execute(Unit.Default);
            }

            public IObservable<bool> CanExecute
            {
                get { return this.deleg.CanExecute; }
            }

            public IDisposable Subscribe(IObserver<Unit> observer)
            {
                return this.deleg.Subscribe(observer);
            }
        }

        private sealed class RxCommandImpl<T> : IRxCommand<T>
        {
            private readonly Subject<T> executeResults = new Subject<T>();

            private readonly IConnectableObservable<bool> canExecute;

            private readonly IDisposable canExecuteDisp;

            int canExecuteLatest = 0;

            internal RxCommandImpl(IObservable<bool> canExecute)
            {
                this.canExecute = canExecute
                    .DistinctUntilChanged()
                    .Do(x =>
                        {
                            System.Threading.Interlocked.Exchange(ref canExecuteLatest, x ? 1 : 0);
                        })
                    .Publish();

                canExecuteDisp = this.canExecute.Connect();
            }

            public void Execute(T parameter)
            {
                if (canExecuteLatest > 0)
                {
                    executeResults.OnNext(parameter);
                }
            }

            public IObservable<bool> CanExecute
            {
                get { return canExecute.StartWith(canExecuteLatest > 0); }
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return executeResults.Subscribe(observer);
            }

            // To Make the compiler happy. Not really necessary and not exposed on the interface intentionally.
            public void Dispose()
            {
                canExecuteDisp.Dispose();
            }
        }
    }
}

