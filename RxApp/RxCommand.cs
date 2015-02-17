using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace RxApp
{
    public interface IRxCommand : IObservable<Unit>, IDisposable
    {
        IObservable<bool> CanExecute { get; }

        void Execute();
    }

    public static class RxCommand
    {
        public static IRxCommand Create() 
        {
            return new RxCommandImpl(Observable.Return(true));
        }

        public static IRxCommand ToCommand(this IObservable<bool> This)
        {
            return new RxCommandImpl(This);
        }

        public static IDisposable InvokeCommand<T>(this IObservable<T> This, IRxCommand command)
        {
            return This.Throttle(x => command.CanExecute)
                .Subscribe(x => command.Execute());
        }

        private sealed class RxCommandImpl : IRxCommand
        {
            private readonly Subject<Unit> executeResults = new Subject<Unit>();

            private readonly IConnectableObservable<bool> canExecute;

            private readonly IDisposable canExecuteDisp;

            bool canExecuteLatest = false;

            internal RxCommandImpl(IObservable<bool> canExecute)
            {
                this.canExecute = canExecute
                    .DistinctUntilChanged()
                    .Do(x =>
                        {
                            this.canExecuteLatest = x;
                        })
                    .Publish();

                canExecuteDisp = this.canExecute.Connect();
            }

            public void Execute()
            {
                executeResults.OnNext(Unit.Default);
            }

            public IObservable<bool> CanExecute
            {
                get { return canExecute.StartWith(canExecuteLatest); }
            }

            public IDisposable Subscribe(IObserver<Unit> observer)
            {
                return executeResults.Subscribe(observer);
            }

            public void Dispose()
            {
                canExecuteDisp.Dispose();
                canExecuteLatest = false;
            }
        }
    }
}

