using System;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class Service
    {
        public static IDisposable Bind(this IServiceControllerModel model, IDisposableService deleg)
        {
            // FIXME: Preconditions or code contracts
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
                subscription.Add (model.Starting.Subscribe(_ => deleg.Start()));
                subscription.Add (model.Stopping.Subscribe(_ =>  deleg.Stop()));
            }

            public void Dispose()
            {
                subscription.Dispose();
                deleg.Dispose();
            }
        }
    }
}

