using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;
using System.Windows.Input;
using ReactiveUI;

namespace RxApp
{
    public interface IService
    {
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
        public static IDisposable Bind(this IServiceControllerModel model, IService service)
        {
            Contract.Requires(model != null);
            Contract.Requires(service != null);

            CompositeDisposable subscription = new CompositeDisposable();

            subscription.Add (model.Start.Subscribe(_ => 
                { 
                    model.CanStart = false;
                    service.Start();
                    model.CanStop = true;
                }));

            subscription.Add (model.Stop.Subscribe(_ =>  
                {
                    model.CanStop = false;
                    service.Stop();
                    model.CanStart = true;
                }));

            return subscription;
        }
    }
}

