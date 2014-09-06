using System;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class Controller
    {
        public static IInitializable DoesNothing()
        {
            return new DoesNothingInitializable();
        }

        private sealed class DoesNothingInitializable : IInitializable
        {
            public void Initialize()
            {
            }

            public void Dispose()
            {
            }
        }

        public static IInitializable Bind(this IServiceControllerModel model, IInitializableService deleg)
        {
            // FIXME: Preconditions or code contracts
            return new ServiceController(model, deleg);
        }

        private sealed class ServiceController : IInitializable
        {
            private readonly IServiceControllerModel model;
            private readonly IInitializableService deleg;
            private readonly CompositeDisposable subscription = new CompositeDisposable();

            internal ServiceController(IServiceControllerModel model, IInitializableService deleg)
            {
                this.model = model;
                this.deleg = deleg;
            }

            public void Initialize()
            {
                deleg.Initialize();
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

