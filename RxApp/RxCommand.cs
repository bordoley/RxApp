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

    public static class RxCommand
    {
        public static IRxCommand Create() 
        {
            return new RxCommandImpl(RxObservable.Return(true));
        }

        public static IRxCommand ToCommand(this IObservable<bool> This)
        {
            return new RxCommandImpl(This);
        }

        public static IDisposable InvokeCommand<T>(this IObservable<T> This, IRxCommand command)
        {
            return This.Subscribe(x => command.Execute());
        }

        private sealed class RxCommandImpl : IRxCommand
        {
            private readonly Subject<Unit> executeResults = new Subject<Unit>();

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

            public void Execute()
            {
                if (canExecuteLatest > 0)
                {
                    executeResults.OnNext(Unit.Default);
                }
            }

            public IObservable<bool> CanExecute
            {
                get { return canExecute.StartWith(canExecuteLatest > 0); }
            }

            public IDisposable Subscribe(IObserver<Unit> observer)
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

