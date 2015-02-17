using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Input;

namespace RxApp
{
    public interface IRxCommand : IObservable<Unit>, ICommand, IDisposable
    {
        IObservable<bool> CanExecuteObservable { get; }

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

        public static IDisposable InvokeCommand<T>(this IObservable<T> This, ICommand command)
        {
            return This.Throttle(x => Observable.FromEventPattern(command, "CanExecuteChanged")
                    .Select(_ => Unit.Default).StartWith(Unit.Default).Where(_ => command.CanExecute(x)))
                .Subscribe(x => command.Execute(x));
        }

        public static IDisposable InvokeCommand<T>(this IObservable<T> This, IRxCommand command)
        {
            return This.Throttle(x => command.CanExecuteObservable.StartWith(command.CanExecute(x)).Where(b => b))
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
                            this.CanExecuteChanged(this, EventArgs.Empty);
                        })
                    .Publish();

                canExecuteDisp = this.canExecute.Connect();
            }

            // FIXME: In ReactiveUI they use the weak event pattern for 
            // all platforms except WPF. Not sure it matters. For reference:
            // https://github.com/reactiveui/ReactiveUI/blob/master/ReactiveUI/ReactiveCommand.cs#L285
            public event EventHandler CanExecuteChanged = (o, e) => {};

            public void Execute()
            {
                executeResults.OnNext(Unit.Default);
            }

            public IObservable<bool> CanExecuteObservable 
            {
                get { return canExecute.StartWith(canExecuteLatest); }
            }

            public IDisposable Subscribe(IObserver<Unit> observer)
            {
                return executeResults.Subscribe(observer);
            }
                
            public bool CanExecute(object parameter)
            {
                return canExecuteLatest;
            }

            public void Execute(object parameter)
            {
                this.Execute();
            }

            public void Dispose()
            {
                canExecuteDisp.Dispose();
                canExecuteLatest = false;
            }
        }
    }
}

