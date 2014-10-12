using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;
using System.Windows.Input;
using ReactiveUI;

namespace RxApp
{
    public interface IService: INotifyPropertyChanged
    {
        bool IsStarted { get; }

        void Start();
        void Stop();
    }

    public interface IServiceViewModel 
    {
        ICommand Start { get; }
        ICommand Stop { get; }
    }

    public interface IServiceControllerModel
    {
        bool CanStart { set; }
        bool CanStop { set; }

        IObservable<object> Start { get; }
        IObservable<object> Stop { get; }
    }

    public static class Service
    {
        public static IDisposable BindAndDispose<TDisposableService> (this IServiceControllerModel model, TDisposableService service)
            where TDisposableService: IDisposable, IService
        {
            CompositeDisposable retval = new CompositeDisposable();
            retval.Add(model.Bind(service));
            retval.Add(service);
            return retval;
        }

        public static IDisposable Bind(this IServiceControllerModel model, IService service)
        {
            Contract.Requires(model != null);
            Contract.Requires(service != null);

            CompositeDisposable subscription = new CompositeDisposable();

            subscription.Add(service.WhenAnyValue(x => x.IsStarted).Subscribe(isStarted => 
                {
                    model.CanStart = !isStarted;
                    model.CanStop = isStarted;
                }));

            subscription.Add (model.Start.Subscribe(_ => service.Start()));
            subscription.Add (model.Stop.Subscribe(_ => service.Stop()));

            return subscription;
        }
    }
}

