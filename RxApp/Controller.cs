using System;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class Controller
    {
        public static IInitable DoesNothing()
        {
            return new NullInitable();
        }

        public static IInitable Lifecycled(ILifecycleControllerModel model, IInitableService deleg)
        {
            // FIXME: Preconditions or code contracts
            return new LifecycleController(model, deleg);
        }

        private sealed class NullInitable : IInitable
        {
            public void Init()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class LifecycleController : IInitable
        {
            private readonly ILifecycleControllerModel model;
            private readonly IInitableService deleg;
            private readonly CompositeDisposable subscription = new CompositeDisposable();

            internal LifecycleController(ILifecycleControllerModel model, IInitableService deleg)
            {
                this.model = model;
                this.deleg = deleg;
            }

            public void Init()
            {
                deleg.Init();
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

