using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;
using ReactiveUI;

namespace RxApp
{
    public interface IService
    {
        void Start();
        void Stop();
    }

    public interface IDisposableService : IDisposable, IService
    {
    }

    public interface IServiceViewModel 
    {
        IReactiveCommand<object> Start { get; }
        IReactiveCommand<object> Stop { get; }
    }

    public interface IServiceControllerModel
    {
        IObservable<object> Start { get; }
        IObservable<object> Stop { get; }
    }

    public interface IServiceModel : IServiceViewModel, IServiceControllerModel
    {
    }

    public static class Service
    {
        public static IDisposable Bind(this IServiceControllerModel model, IDisposableService deleg)
        {
            Contract.Requires(model != null);
            Contract.Requires(deleg != null);

            var retval = new ServiceControllerBinding(model, deleg);
            retval.Initialize();
            return retval;
        }

        private sealed class ServiceControllerBinding : IInitializable
        {
            private readonly IServiceControllerModel model;
            private readonly IDisposableService deleg;
            private readonly CompositeDisposable subscription = new CompositeDisposable();

            internal ServiceControllerBinding(IServiceControllerModel model, IDisposableService deleg)
            {
                this.model = model;
                this.deleg = deleg;
            }

            public void Initialize()
            {
                subscription.Add (model.Start.Subscribe(_ => deleg.Start()));
                subscription.Add (model.Stop.Subscribe(_ =>  deleg.Stop()));
            }

            public void Dispose()
            {
                subscription.Dispose();
                deleg.Dispose();
            }
        }
    }
}

