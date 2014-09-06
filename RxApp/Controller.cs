using System;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class Controller
    {
        public static IInitializable DoesNothing()
        {
            return new NullInitializable();
        }

        public static IInitializable Lifecycled(ILifecycleControllerModel model, IInitializableService deleg)
        {
            // FIXME: Preconditions or code contracts
            return new LifecycleController(model, deleg);
        }

        private sealed class NullInitializable : IInitializable
        {
            public void Initialize()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class LifecycleController : IInitializable
        {
            private readonly ILifecycleControllerModel model;
            private readonly IInitializableService deleg;
            private readonly CompositeDisposable subscription = new CompositeDisposable();

            internal LifecycleController(ILifecycleControllerModel model, IInitializableService deleg)
            {
                this.model = model;
                this.deleg = deleg;
            }

            public void Initialize()
            {
                deleg.Initialize();
                subscription.Add (model.Resuming.Subscribe(_ => deleg.Start()));
                subscription.Add (model.Pausing.Subscribe(_ =>  deleg.Stop()));
            }

            public void Dispose()
            {
                subscription.Dispose();
                deleg.Dispose();
            }
        }
    }
}

