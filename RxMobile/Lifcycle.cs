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

    public sealed class LifecycleController : IService
    {
        public static IService Create(ILifecycleControllerModel model, Action onPaused, Action onResumed)
        {
            // FIXME: Preconditions or code contracts
            return new LifecycleController(model, onPaused, onResumed);
        }

        private readonly ILifecycleControllerModel model;
        private readonly Action onPaused;
        private readonly Action onResumed;

        private bool paused = true;
        private IDisposable subscription;

        private LifecycleController(ILifecycleControllerModel model, Action onPaused, Action onResumed)
        {
            this.model = model;
            this.onPaused = onPaused;
            this.onResumed = onResumed;
        }

        public void Start()
        {
            if (subscription != null)
            {
                throw new NotSupportedException("Trying to call start more than once without first calling stop");
            }

            var newSubscription = new CompositeDisposable();

            newSubscription.Add (
                model.Resuming.Subscribe(_ => 
                    {
                        paused = true;
                        onResumed();
                    }));

            newSubscription.Add (
                model.Pausing.Subscribe(_ => 
                    {
                        paused = true;
                        onPaused();
                    }));
                    
            subscription = newSubscription;
        }

        public void Stop()
        {
            if (subscription != null)
            {
                subscription.Dispose();
                subscription = null;
            }
             
            if (!paused)
            {
                onPaused();
            }
        }
    }
}