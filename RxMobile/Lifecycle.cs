using System;
using System.Reactive.Disposables;
using ReactiveUI;

namespace RxMobile
{
    public interface ILifecycleViewModel 
    {
        IReactiveCommand<object> Pausing { get; }
        IReactiveCommand<object> Resuming { get; }
    }

    public interface ILifecycleControllerModel
    {
        IObservable<object> Pausing { get; }
        IObservable<object> Resuming { get; }
    }

    public interface ILifecycleModel : ILifecycleViewModel, ILifecycleControllerModel
    {
    }

    public interface ILifecycleController : IDisposable
    {
        void Pause();
        void Resume();
    }

    public sealed class LifecycleController : IController
    {
        public static IController Create(ILifecycleControllerModel model, ILifecycleController deleg)
        {
            // FIXME: Preconditions or code contracts
            return new LifecycleController(model, deleg);
        }

        private readonly ILifecycleControllerModel model;
        private readonly ILifecycleController deleg;
        private readonly CompositeDisposable subscription = new CompositeDisposable();

        private LifecycleController(ILifecycleControllerModel model, ILifecycleController deleg)
        {
            this.model = model;
            this.deleg = deleg;
        }

        public void Initialize()
        {
            subscription.Add (model.Resuming.Subscribe(_ => deleg.Resume()));
            subscription.Add (model.Pausing.Subscribe(_ =>  deleg.Pause()));
        }

        public void Dispose()
        {
            subscription.Dispose();
            deleg.Dispose();
        }
    }
}