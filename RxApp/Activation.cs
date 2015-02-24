using System;
using System.Diagnostics.Contracts;
using System.Reactive.Disposables;

namespace RxApp
{
    public static class Service
    {
        public static IDisposable Bind(this IActivationControllerModel model, Func<IDisposable> provideService)
        {
            Contract.Requires(model != null);
            Contract.Requires(provideService != null);

            IDisposable service = null;

            return Disposable.Compose(
                model.Start.Subscribe(_ =>  
                    {
                        if (service == null)
                        {
                            model.CanStart.Value = false;
                            service = provideService ();
                        }
                    }),

                model.Stop.Subscribe(_ => 
                    {
                        if (service != null)
                        {
                            service.Dispose();
                        }

                        model.CanStart.Value = true;
                        service = null;
                    })
            );
        }
    }
}

